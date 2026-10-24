using System;

namespace Riftrail
{
    public enum BattlePhase { Fighting, Victory, Defeat, Escaped }
    public enum EncounterKind { Farming, Boss }
    public enum MonsterKind { Slime, Golem, Orc, Goblin, Guardian, SlimeKing, GoblinChief, OrcChief, AncientGolem, Mushroom, Skeleton, Dokebi, Snowball, Ghost, Wyvern, MushroomKing, SkeletonKnight, DokebiKing, YetiChief, GhostGentleman, TreasureDragon }
    public enum HeroKind { SwordMaster, Wuxia, Kunoichi }

    public sealed class EnemyState
    {
        public MonsterKind Kind { get; }
        public string Name { get; }
        public long MaxHealth { get; }
        public long Health { get; private set; }
        public long GoldReward { get; }
        public long LastHitPower {get;private set;}
        public int RageLevel { get; }
        public long Shield {get;private set;}
        public long MaxShield {get;}
        public bool Revived {get;private set;}
        public int CoinDrops {get;private set;}
        public double Elapsed {get;internal set;}
        public bool IsSleeping => StageCatalog.IsMushroom(Kind) && Math.Round(Elapsed,6)%4 >= 2.5-1e-9;
        public bool IsExposed => !StageCatalog.IsGhost(Kind) || Math.Round(Elapsed,6)%3 < 1.5-1e-9;
        public bool IsArmored => StageCatalog.IsDragon(Kind) && Health <= MaxHealth*.3;
        internal EnemyState(string name, long health, MonsterKind kind, long reward, int rage)
        {
            Name=name; Kind=kind; Health=MaxHealth=health; GoldReward=reward; RageLevel=rage;
            Shield=MaxShield=StageCatalog.IsIce(kind)?Math.Max(1,health/2):0;
        }
        internal long Hit(long power,bool heavy=false,bool critical=false)
        {
            LastHitPower=0;
            if(!IsExposed) return 0;
            if(Shield>0)
            {
                LastHitPower=heavy?(long)Math.Min(long.MaxValue/4d,power*2d):power;
                var damage=Math.Min(Shield,LastHitPower);
                Shield-=damage; return damage;
            }
            if(IsSleeping) power=(long)Math.Min(long.MaxValue/4d,power*2d);
            if(IsArmored) power=Math.Max(1,(long)Math.Round(power*(critical?.75:.5)));
            LastHitPower=Kind==MonsterKind.Slime?1:power;
            var dealt=Math.Min(LastHitPower,Health);
            Health-=dealt;
            if(Health==0 && StageCatalog.IsSkeleton(Kind) && !Revived)
            { Revived=true; Health=Math.Max(1,(long)Math.Round(MaxHealth*.35)); }
            return dealt;
        }
        internal long DropHitGold()
        {
            if(!StageCatalog.IsDokebi(Kind)||CoinDrops>=5) return 0;
            CoinDrops++; return Math.Max(1,GoldReward/20);
        }
    }

    public readonly struct BattleAttack
    {
        public readonly long Damage, DisplayDamage;
        public readonly bool KillingBlow, FollowUp, IsCritical;
        public readonly HeroKind Hero;
        internal BattleAttack(long damage, bool killingBlow, HeroKind hero, bool followUp, bool critical = false,long? displayDamage=null)
        { DisplayDamage=displayDamage??damage; IsCritical = critical; Damage = damage; KillingBlow = killingBlow; Hero = hero; FollowUp = followUp; }
    }

    /// <summary>Fixed-step combat, monster traits and hero timing, independent of Unity.</summary>
    public sealed class BattleSimulation
    {
        public const double StepSeconds = .05;
        public const double BossDuration = 15;
        public const double GoblinEscapeTime = 3;
        public const double GoblinBonusTime = 2;
        public const int RequiredKills = 5;
        public const int MaxUpgradeLevel = BattleBalance.MaxUpgradeLevel;
        private double accumulator, attackCooldown, followUpRemaining = -1;
        private int encounterAttacks;
        private RebirthProgress rebirth;
        private readonly int[] growthLevels = new int[5];
        private uint criticalRandom = 0xA341316Cu;
        public EnemyState Enemy { get; private set; }
        public HeroKind Hero { get; private set; }
        public int Stage { get; private set; }
        public int BattleId { get; private set; }
        public int FarmKills { get; private set; }
        public int BossesCleared { get; private set; }
        public int Defeats { get; private set; }
        public int Escapes { get; private set; }
        public int OrcRage { get; private set; }
        public int UpgradeLevel { get; private set; }
        public long Gold { get; private set; }
        public long TotalGoldEarned { get; private set; }
        public bool IsPaused { get; set; }
        public bool LastKillWasBonus { get; private set; }
        public EncounterKind Encounter { get; private set; }
        public BattlePhase Phase { get; private set; }
        public double TransitionRemaining { get; private set; }
        public double BossTimeRemaining { get; private set; }
        public double EnemyElapsed { get; private set; }
        public double GoblinTimeRemaining => Math.Max(0, GoblinEscapeTime - EnemyElapsed);
        public string Notice { get; private set; }
        public int RunHighestClearedStage { get; private set; }
        public long RebirthPoints => rebirth.points;
        public int TotalRebirths => rebirth.rebirths;
        public int PermanentAttackLevel => rebirth.attackLevel;
        public int PermanentGoldLevel => rebirth.goldLevel;
        public int LifetimeBestStage => rebirth.bestClearedStage;
        public int RebirthReward => Math.Min(BattleBalance.StatStageLimit, RunHighestClearedStage) / 5;
        public bool CanRebirth => RebirthReward > 0 && !(Encounter == EncounterKind.Boss && Phase == BattlePhase.Fighting)
            && rebirth.rebirths < int.MaxValue && rebirth.totalEarned <= long.MaxValue - RebirthReward;
        public double AttackExactAtLevel(int level)
        {
            var scaled=Math.Min(MaxUpgradeLevel,Math.Max(0,level))/(double)BattleBalance.GrowthSubdivision;var low=(int)scaled;
            var a=Math.Min(long.MaxValue/128d,Math.Round(RebirthProgress.Apply(BattleBalance.LegacyAttack(low),rebirth.attackLevel)*(1+.03*rebirth.relicAttack)));
            var b=Math.Min(long.MaxValue/128d,Math.Round(RebirthProgress.Apply(BattleBalance.LegacyAttack(low+1),rebirth.attackLevel)*(1+.03*rebirth.relicAttack)));
            return a+(b-a)*(scaled-low);
        }
        public long AttackAtLevel(int level)=>(long)Math.Round(AttackExactAtLevel(level));
        public long AttackPower=>AttackAtLevel(UpgradeLevel);
        public long NextAttackPower => AttackAtLevel(Math.Min(MaxUpgradeLevel, UpgradeLevel + 1));
        public double SpeedMultiplier => (1 + .002 * GrowthLevel(RebirthUpgrade.AttackSpeed)) * (1 + .02 * PermanentLevel(RebirthUpgrade.AttackSpeed)) * (1+.005*rebirth.relicSpeed);
        public double CritChance => Math.Min(.75, .0005 * GrowthLevel(RebirthUpgrade.CritChance) + .0025 * PermanentLevel(RebirthUpgrade.CritChance));
        public double CritMultiplier => 1.5 + .005 * GrowthLevel(RebirthUpgrade.CritDamage) + .05 * PermanentLevel(RebirthUpgrade.CritDamage);
        public double GoldMultiplier => (1 + .005 * GrowthLevel(RebirthUpgrade.Gold)) * (1 + .10 * PermanentGoldLevel) * (1+.03*rebirth.relicGold);
        public double AttackInterval => BattleBalance.HeroInterval(Hero) / SpeedMultiplier;
        private double FirstAttackDelay => BattleBalance.FirstAttack(Hero) / SpeedMultiplier;
        public double DamagePerSecond => BattleBalance.StrikeDamage(AttackPower, Hero, StageCatalog.Boss(Stage))
            * (Hero == HeroKind.Kunoichi ? 2d : 1d) / AttackInterval * (1 + CritChance * (CritMultiplier - 1));
        public long KillReward => (long)Math.Round(Enemy.GoldReward * GoldMultiplier) * (Enemy.Kind == MonsterKind.Goblin && EnemyElapsed <= GoblinBonusTime + 1e-9 ? 2 : 1);
        public long BossReward => (long)Math.Round(BattleBalance.BossGold(Stage) * GoldMultiplier);
        public bool IsEliteBoss => BattleBalance.IsElite(Stage);
        private int cachedStage=-1,cachedRage; private HeroKind cachedHero; private double cachedSpeed; private long cachedTarget;
        public long BossTargetPower
        {
            get {
                if(cachedStage!=Stage||cachedHero!=Hero||cachedSpeed!=SpeedMultiplier||cachedRage!=OrcRage)
                { cachedStage=Stage; cachedHero=Hero; cachedSpeed=SpeedMultiplier; cachedRage=OrcRage;
                    cachedTarget=BattleBalance.RequiredBossPower(Stage,Hero,SpeedMultiplier,OrcRage); }
                return cachedTarget;
            }
        }
        public bool HasBossPower => AttackPower >= BossTargetPower;
        public long UpgradeCost => UpgradeLevel >= MaxUpgradeLevel ? 0 : BattleBalance.Cost(UpgradeLevel);
        public bool CanUpgrade => !IsPaused && UpgradeLevel < MaxUpgradeLevel && Gold >= UpgradeCost;
        public bool CanChallengeBoss => !IsPaused && Encounter == EncounterKind.Farming
            && FarmKills >= RequiredKills;
        public event Action<RelicKind> RelicFound;
        public RelicKind? LastRelicDrop { get; private set; }
        public int RelicCount(RelicKind kind) => kind==RelicKind.Blade?rebirth.relicAttack:kind==RelicKind.Feather?rebirth.relicSpeed:rebirth.relicGold;
        public double LastRelicBefore {get;private set;}
        public double LastRelicAfter {get;private set;}
        private double RelicStat(RelicKind kind) => kind==RelicKind.Blade?AttackPower:kind==RelicKind.Feather?SpeedMultiplier:GoldMultiplier;
        public int TotalRelics => rebirth.relicAttack+rebirth.relicSpeed+rebirth.relicGold;
        private void RollRelic()
        {
            LastRelicDrop=null;
            if(rebirth.relicRolls==int.MaxValue) return;
            var hash=unchecked(((uint)rebirth.relicSeed ^ (uint)++rebirth.relicRolls)*747796405u+2891336453u);
            hash=(hash^(hash>>16))*2246822519u; hash^=hash>>13;
            if(hash%100>=30) return;
            for(var i=0;i<3;i++)
            {
                var kind=(RelicKind)((hash/100+(uint)i)%3);
                if(RelicCount(kind)>=1000) continue;
                LastRelicBefore=RelicStat(kind);
                if(kind==RelicKind.Blade) rebirth.relicAttack++;
                else if(kind==RelicKind.Feather) rebirth.relicSpeed++;
                else rebirth.relicGold++;
                LastRelicAfter=RelicStat(kind);
                LastRelicDrop=kind; RelicFound?.Invoke(kind); return;
            }
        }
        public event Action<BattleAttack> Attacked;
        public event Action<long> GoldEarned;
        public event Action PermanentProgressChanged;
        public BattleSimulation(RebirthProgress progress = null)
        {
            if (progress != null && !progress.IsValid) throw new ArgumentException("Invalid rebirth progress.");
            rebirth = progress?.Copy() ?? new RebirthProgress();
            if(rebirth.relicSeed==0) rebirth.relicSeed=Math.Max(1,Guid.NewGuid().GetHashCode() & int.MaxValue);
            ResetRun(false);
        }
        public const int MaximumOfflineSeconds=8*60*60;
        public long SavedUtcSeconds {get;private set;}
        public long PendingOfflineGold {get;private set;}
        public long PendingOfflineSeconds {get;private set;}
        public long SaveRevision {get;private set;}
        public void MarkSavedAt(long utcSeconds) {SavedUtcSeconds=Math.Max(SavedUtcSeconds,Math.Max(0,Math.Min(253402300799L,utcSeconds)));}
        public bool AccrueOffline(long utcSeconds)
        {
            utcSeconds=Math.Max(0,Math.Min(253402300799L,utcSeconds));
            var elapsed=SavedUtcSeconds>0?Math.Max(0,utcSeconds-SavedUtcSeconds):0;
            MarkSavedAt(utcSeconds);
            if(elapsed<60)return false;
            var seconds=(int)Math.Min(elapsed,MaximumOfflineSeconds-PendingOfflineSeconds);
            if(seconds<=0)return false;
            var amount=EstimateOfflineGold(ExportGame(),seconds);
            var headroom=Math.Max(0,Math.Min(long.MaxValue-Gold,long.MaxValue-TotalGoldEarned)-PendingOfflineGold);
            amount=Math.Min(amount,headroom);
            if(amount<=0)return false;
            PendingOfflineGold+=amount;PendingOfflineSeconds+=seconds;SaveRevision++;return true;
        }
        public bool TryClaimOffline()
        {
            if(PendingOfflineGold<=0)return false;
            var amount=PendingOfflineGold;PendingOfflineGold=PendingOfflineSeconds=0;SaveRevision++;
            Reward(amount);Notice="오프라인 골드를 수령했습니다.";return true;
        }
        internal static long EstimateOfflineGold(GameSave save,int seconds)
        {
            if(seconds<=0)return 0;
            var b=Restore(save);b.Gold=b.TotalGoldEarned=0;b.AutomaticGrowth=null;b.AutoBossAttempted=true;b.FarmKills=5;b.IsPaused=false;
            var sample=Math.Min(seconds,300);b.Advance(sample);
            if(b.TotalGoldEarned==0 && seconds>sample) {b.Advance(seconds-sample);return Math.Max(0,b.TotalGoldEarned);}
            return (long)Math.Min(long.MaxValue/4d,Math.Floor((double)b.TotalGoldEarned/sample*seconds));
        }
        public GameSave ExportGame()
        {
            var cleared=Encounter==EncounterKind.Boss && Phase==BattlePhase.Victory;
            var levels=new int[5];for(var i=0;i<5;i++)levels[i]=GrowthLevel((RebirthUpgrade)i);
            return new GameSave{savedUtcSeconds=SavedUtcSeconds,pendingOfflineGold=PendingOfflineGold,pendingOfflineSeconds=PendingOfflineSeconds,permanent=rebirth.Copy(),stage=cleared?Stage+1:Stage,hero=(int)Hero,
                gold=Gold,totalGold=TotalGoldEarned,levels=levels,automatic=AutomaticGrowth.HasValue && GrowthLevel(AutomaticGrowth.Value)<GrowthLimit(AutomaticGrowth.Value)?(int)AutomaticGrowth.Value:-1,
                farmKills=cleared?0:FarmKills,rage=cleared?0:OrcRage,bossAttempted=cleared?false:AutoBossAttempted,
                highestCleared=RunHighestClearedStage,bossesCleared=BossesCleared,defeats=Defeats,escapes=Escapes,criticalState=criticalRandom};
        }
        public static BattleSimulation Restore(GameSave save)
        {
            if(save==null || !save.IsValid)throw new ArgumentException("Invalid game save.");
            save=save.ToCurrent();
            var b=new BattleSimulation(save.permanent);
            b.Stage=save.stage;b.Hero=(HeroKind)save.hero;b.Gold=save.gold;b.TotalGoldEarned=save.totalGold;
            b.UpgradeLevel=save.levels[0];Array.Copy(save.levels,b.growthLevels,5);
            b.AutomaticGrowth=save.automatic<0?(RebirthUpgrade?)null:(RebirthUpgrade)save.automatic;
            b.FarmKills=save.farmKills;b.OrcRage=save.rage;b.AutoBossAttempted=save.bossAttempted;
            b.RunHighestClearedStage=save.highestCleared;b.BossesCleared=save.bossesCleared;b.Defeats=save.defeats;b.Escapes=save.escapes;
            b.SavedUtcSeconds=save.savedUtcSeconds;b.PendingOfflineGold=save.pendingOfflineGold;b.PendingOfflineSeconds=save.pendingOfflineSeconds;
            b.criticalRandom=(uint)save.criticalState;b.BeginEncounter(EncounterKind.Farming);
            b.Notice="저장한 모험을 이어갑니다.";return b;
        }
        public bool CanAbandonBoss => !IsPaused && Encounter==EncounterKind.Boss && Phase==BattlePhase.Fighting;
        public bool TryAbandonBoss()
        {
            if(!CanAbandonBoss)return false;
            AutoBossAttempted=true;BeginEncounter(EncounterKind.Farming);SaveRevision++;
            Notice="보스 도전을 포기하고 일반 사냥으로 돌아왔습니다.";return true;
        }
        public RebirthProgress ExportPermanentProgress() => rebirth.Copy();
        public int PermanentUpgradeCost(RebirthUpgrade kind) => RebirthProgress.Cost(PermanentLevel(kind));
        public bool CanBuyPermanent(RebirthUpgrade kind)
        {
            if (!Enum.IsDefined(typeof(RebirthUpgrade), kind)) return false;
            var level = PermanentLevel(kind);
            return level < RebirthProgress.MaxLevel && rebirth.points >= PermanentUpgradeCost(kind);
        }
        public bool TryBuyPermanent(RebirthUpgrade kind)
        {
            if (!CanBuyPermanent(kind)) return false;
            SaveRevision++;
            rebirth.points -= PermanentUpgradeCost(kind);
            rebirth.Increase(kind);
            PermanentProgressChanged?.Invoke();
            return true;
        }
        public int PermanentLevel(RebirthUpgrade kind) => rebirth.Level(kind);
        public int GrowthLevel(RebirthUpgrade kind) => kind == RebirthUpgrade.Attack ? UpgradeLevel : growthLevels[(int)kind];
        public int GrowthLimit(RebirthUpgrade kind) => kind == RebirthUpgrade.Attack ? MaxUpgradeLevel : BattleBalance.MaxGrowthLevel;
        public long GrowthCost(RebirthUpgrade kind) => kind == RebirthUpgrade.Attack ? UpgradeCost
            : GrowthLevel(kind) >= GrowthLimit(kind) ? 0 : BattleBalance.GrowthCost(kind, GrowthLevel(kind));
        public bool CanBuyGrowth(RebirthUpgrade kind) => Enum.IsDefined(typeof(RebirthUpgrade),kind) && !IsPaused
            && GrowthLevel(kind) < GrowthLimit(kind) && Gold >= GrowthCost(kind);
        public RebirthUpgrade? AutomaticGrowth {get;private set;}
        public bool AutoBossAttempted {get;private set;}
        public bool ToggleAutomaticGrowth(RebirthUpgrade kind)
        {
            if(IsPaused || !Enum.IsDefined(typeof(RebirthUpgrade),kind))return false;
            if(AutomaticGrowth==kind) {AutomaticGrowth=null;SaveRevision++;return true;}
            if(GrowthLevel(kind)>=GrowthLimit(kind))return false;
            AutomaticGrowth=kind;SaveRevision++;SpendAutomaticGrowth();return true;
        }
        private void SpendAutomaticGrowth()
        {
            if(IsPaused || !AutomaticGrowth.HasValue)return;
            var kind=AutomaticGrowth.Value;
            while(CanBuyGrowth(kind))TryBuyGrowth(kind);
            if(GrowthLevel(kind)>=GrowthLimit(kind))AutomaticGrowth=null;
        }
        public bool TryBuyGrowth(RebirthUpgrade kind)
        {
            if (!CanBuyGrowth(kind)) return false;
            if (kind == RebirthUpgrade.Attack) return TryUpgrade();
            Gold -= GrowthCost(kind); growthLevels[(int)kind]++;SaveRevision++;
            Notice = "Growth increased. Your next battle is stronger!";
            return true;
        }
        // amount=0 buys the affordable maximum; fixed quantities are atomic and clamp only at the level cap.
        public int GrowthPurchaseCount(RebirthUpgrade kind,int amount,out long cost)
        {
            cost=0;
            if(!Enum.IsDefined(typeof(RebirthUpgrade),kind) || amount<0) return 0;
            var level=GrowthLevel(kind); var remaining=GrowthLimit(kind)-level;
            var wanted=amount==0?remaining:Math.Min(amount,remaining);
            var count=0;
            for(;count<wanted;count++)
            {
                var price=kind==RebirthUpgrade.Attack?BattleBalance.Cost(level+count):BattleBalance.GrowthCost(kind,level+count);
                if(amount==0 && price>Gold-cost) break;
                if(price>long.MaxValue-cost) return 0;
                cost+=price;
            }
            if(count==0 && remaining>0) cost=GrowthCost(kind);
            return count;
        }
        public bool TryBuyGrowthBatch(RebirthUpgrade kind,int amount)
        {
            var count=GrowthPurchaseCount(kind,amount,out var cost);
            if(IsPaused || count==0 || Gold<cost) return false;
            for(var i=0;i<count;i++) TryBuyGrowth(kind);
            return true;
        }
        public bool TryRebirth()
        {
            if (!CanRebirth) return false;
            var reward = RebirthReward;
            rebirth.points += reward;
            rebirth.totalEarned += reward;
            rebirth.rebirths++;
            var paused = IsPaused;
            ResetRun(true);
            IsPaused = paused;
            Notice = $"REBORN! +{reward} rebirth points. Permanent growth retained.";
            PermanentProgressChanged?.Invoke();
            return true;
        }

        public void Reset()
        {
            rebirth = new RebirthProgress{relicSeed=Math.Max(1,Guid.NewGuid().GetHashCode() & int.MaxValue)};
            SavedUtcSeconds=PendingOfflineGold=PendingOfflineSeconds=0;
            ResetRun(false);
        }
        private void ResetRun(bool keepHero)
        {
            SaveRevision++;
            Stage = 1;
            AutomaticGrowth=null; AutoBossAttempted=false;
            Array.Clear(growthLevels, 0, growthLevels.Length);
            criticalRandom = 0xA341316Cu;
            if (!keepHero) Hero = HeroKind.SwordMaster;
            RunHighestClearedStage = 0;
            FarmKills = BossesCleared = Defeats = Escapes = OrcRage = UpgradeLevel = 0;
            Gold = TotalGoldEarned = 0;
            IsPaused = false; accumulator = 0;
            Notice = "Hunt, grow, and choose the right fighting style.";
            BeginEncounter(EncounterKind.Farming);
        }

        public bool TrySelectHero(HeroKind kind)
        {
            if (!Enum.IsDefined(typeof(HeroKind), kind)) return false;
            if (Hero == kind) return true;
            Hero = kind;SaveRevision++;
            followUpRemaining = -1;
            // A swap never produces an attack or shortens an already running cooldown.
            attackCooldown = encounterAttacks == 0 && EnemyElapsed <= 1e-9
                ? FirstAttackDelay : Math.Max(attackCooldown, FirstAttackDelay);
            return true;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade) return false;
            Gold -= UpgradeCost; UpgradeLevel++;SaveRevision++;
            Notice = "Attack increased. Your next strike is stronger!";
            return true;
        }
        public bool TryChallengeBoss()
        {
            if (!CanChallengeBoss) return false;
            Notice = "Defeat the region boss before time runs out!";
            BeginEncounter(EncounterKind.Boss);
            return true;
        }
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsPaused) return;
            accumulator += seconds;
            while (accumulator + 1e-9 >= StepSeconds)
            { accumulator -= StepSeconds; Tick(); }
        }
        private void BeginEncounter(EncounterKind kind)
        {
            BattleId++; Encounter = kind; Phase = BattlePhase.Fighting;
            if(kind==EncounterKind.Boss){AutoBossAttempted=true;SaveRevision++;}
            TransitionRemaining = EnemyElapsed = 0;
            LastRelicDrop = null;
            LastKillWasBonus = false; encounterAttacks = 0; followUpRemaining = -1;
            BossTimeRemaining = kind == EncounterKind.Boss ? BossDuration : 0;
            attackCooldown = FirstAttackDelay;
            var monster = kind == EncounterKind.Boss ? StageCatalog.Boss(Stage) : StageCatalog.Monster(Stage);
            Enemy = new EnemyState(StageCatalog.Name(monster), BattleBalance.Health(Stage, monster, OrcRage), monster,
                BattleBalance.Reward(Stage, monster, OrcRage), monster == MonsterKind.Orc || monster == MonsterKind.OrcChief ? OrcRage : 0);
        }
        private void Reward(long amount)
        { SaveRevision++;Gold += amount; TotalGoldEarned += amount; GoldEarned?.Invoke(amount); SpendAutomaticGrowth(); }

        private void Strike(bool followUp)
        {
            // Independent deterministic roll per hit; slime always keeps its fixed damage rule.
            criticalRandom ^= criticalRandom << 13; criticalRandom ^= criticalRandom >> 17; criticalRandom ^= criticalRandom << 5;
            var critical = Enemy.Kind != MonsterKind.Slime && criticalRandom / 4294967296d < CritChance;
            var raw = BattleBalance.StrikeDamage(AttackPower, Hero, Enemy.Kind);
            var damage = Enemy.Hit(critical ? (long)Math.Min(long.MaxValue / 4d, Math.Round(raw * CritMultiplier)) : raw, Hero==HeroKind.Wuxia,critical);
            if(damage>0)
            {
                var drop=Enemy.DropHitGold();
                if(drop>0) Reward((long)Math.Round(drop*GoldMultiplier));
            }
            encounterAttacks++;
            Attacked?.Invoke(new BattleAttack(damage, Enemy.Health == 0, Hero, followUp, critical,Enemy.LastHitPower));
            if (Enemy.Health != 0) return;
            Phase = BattlePhase.Victory;
            if (Encounter == EncounterKind.Boss)
            {
                BossesCleared++; OrcRage = 0;
                RunHighestClearedStage = Math.Max(RunHighestClearedStage, Stage);
                if (RunHighestClearedStage > rebirth.bestClearedStage)
                {
                    rebirth.bestClearedStage = RunHighestClearedStage;
                }
                Notice = "보스를 처치했습니다! 다음 지역으로 이동합니다.";
                Reward(BossReward);
                RollRelic();
                PermanentProgressChanged?.Invoke();
            }
            else
            {
                FarmKills = Math.Min(RequiredKills, FarmKills + 1);
                LastKillWasBonus = Enemy.Kind == MonsterKind.Goblin && EnemyElapsed <= GoblinBonusTime + 1e-9;
                if (Enemy.Kind == MonsterKind.Orc) OrcRage = Math.Min(5, OrcRage + 1);
                Notice = LastKillWasBonus ? "QUICK CATCH! Double goblin gold." : "Monster defeated.";
                Reward(KillReward);
            }
            followUpRemaining = -1;
            TransitionRemaining = Encounter == EncounterKind.Boss ? 1.5 : .45;
        }
        private void Tick()
        {
            SpendAutomaticGrowth();
            if(Encounter==EncounterKind.Farming && FarmKills>=RequiredKills && !AutoBossAttempted)
            {BeginEncounter(EncounterKind.Boss);return;}

            if (Phase != BattlePhase.Fighting)
            {
                TransitionRemaining = Math.Max(0, TransitionRemaining - StepSeconds);
                if (TransitionRemaining <= 1e-9)
                {
                    if (Encounter == EncounterKind.Boss && Phase == BattlePhase.Victory)
                    { Stage++; SaveRevision++; FarmKills = 0; AutoBossAttempted=false; Notice = "New stage reached. The journey continues."; }
                    else if (Phase == BattlePhase.Defeat) Notice = "Time ran out. Upgrade your attack and try again.";
                    BeginEncounter(EncounterKind.Farming);
                }
                return;
            }
            EnemyElapsed += StepSeconds;
            Enemy.Elapsed = EnemyElapsed;
            attackCooldown -= StepSeconds;
            if (followUpRemaining >= 0)
            {
                followUpRemaining -= StepSeconds;
                if (followUpRemaining <= 1e-9)
                {
                    followUpRemaining = -1;
                    Strike(true);
                    if (Phase != BattlePhase.Fighting) return;
                }
            }
            if (attackCooldown <= 1e-9)
            {
                attackCooldown += AttackInterval;
                Strike(false);
                if (Phase != BattlePhase.Fighting) return;
                if (Hero == HeroKind.Kunoichi) followUpRemaining = .12 / SpeedMultiplier;
            }
            // Final-step hits can win before escape/timeout is resolved.
            if (Encounter == EncounterKind.Farming && Enemy.Kind == MonsterKind.Goblin && GoblinTimeRemaining <= 1e-9)
            {
                Escapes++;SaveRevision++; Phase = BattlePhase.Escaped; TransitionRemaining = .5;
                Notice = "The goblin escaped! No gold or discovery progress.";
            }
            if (Encounter != EncounterKind.Boss) return;
            BossTimeRemaining = Math.Max(0, BossTimeRemaining - StepSeconds);
            if (BossTimeRemaining > 1e-9) return;
            Defeats++;SaveRevision++; Phase = BattlePhase.Defeat; TransitionRemaining = 1.5;
            Notice = "TIME UP / Your gold and attack upgrades are kept.";
        }
    }
}

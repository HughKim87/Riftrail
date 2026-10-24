using System;
using System.Text;
using UnityEngine;

namespace Riftrail.Editor
{
    public static class BattleChecks
    {
        public static void Run()
        {
            CheckSubdivisionAndReset(); CheckOfflineRewards(); CheckRunSaveAndAbandon(); CheckAutomaticGrowth(); CheckAutomaticBoss(); CheckNumbersAndOverkill(); CheckRelics(); CheckBossDuringKillTransition(); CheckBatchGrowth(); CheckExtraTraits(); CheckStages(); CheckTraits(); CheckHeroesAndTiming(); CheckGrowthAndSave(); CheckProgression();
            Debug.Log("RIFTRAIL combat checks passed: stage families, ten regional bosses, traits, heroes, growth, rebirth, persistence, deterministic timing.");
        }
        private static void CheckSubdivisionAndReset()
        {
            var profile=new RebirthProgress{attackLevel=7,goldLevel=3,speedLevel=4,critChanceLevel=5,critDamageLevel=6,relicAttack=2,relicSpeed=3,relicGold=4,relicSeed=42};
            var old=new BattleSimulation(profile).ExportGame();old.version=1;old.levels=new[]{100,100,100,100,100};old.automatic=-1;
            Require(GameSaveStore.TryDecode(JsonUtility.ToJson(old),out var upgraded) && upgraded.version==2,"Version one save upgrades to the subdivided schema");
            var b=BattleSimulation.Restore(upgraded);
            for(var i=0;i<5;i++)Require(b.GrowthLevel((RebirthUpgrade)i)==1000,"Existing gold growth levels migrate by ten");
            Require(b.AttackPower==(long)Math.Round(RebirthProgress.Apply(BattleBalance.LegacyAttack(100),7)*1.06)
                && Math.Abs(b.SpeedMultiplier-3*1.08*1.015)<1e-9 && Math.Abs(b.CritChance-.5125)<1e-9
                && Math.Abs(b.CritMultiplier-6.8)<1e-9 && Math.Abs(b.GoldMultiplier-6*1.3*1.12)<1e-9,"New level 1000 retains every old level 100 stat including permanent/relic bonuses");
            Require(b.PermanentAttackLevel==7 && RebirthProgress.MaxLevel==100,"Rebirth growth is unchanged");
            Require(GameSaveStore.TryDecode(GameSaveStore.Encode(upgraded),out var again) && again.levels[0]==1000,"Reload never scales migrated levels a second time");
            for(var level=100;level<=1000;level+=100)
            {
                Require(BattleBalance.BarrierFactor(level)==2*BattleBalance.BarrierFactor(level-1),"Every hundredth level doubles the extra gold price multiplier");
                Require(BattleBalance.Cost(level)>=2*BattleBalance.Cost(level-1)-1,"Attack price shows the milestone wall");
                Require(BattleBalance.GrowthCost(RebirthUpgrade.Gold,level)>=2*BattleBalance.GrowthCost(RebirthUpgrade.Gold,level-1)-1,"Other gold stats share the milestone wall");
            }
            var start=new BattleSimulation();Require(start.AttackExactAtLevel(1)>start.AttackExactAtLevel(0) && start.GrowthLimit(RebirthUpgrade.Attack)==1800 && start.GrowthLimit(RebirthUpgrade.Gold)==1000,"Fractional attack progress and new caps are exposed");
            var key="Riftrail.Test.Reset."+Guid.NewGuid().ToString("N");var legacy=key+".Legacy";
            try {GameSaveStore.Save(b.ExportGame(),key);RebirthSaveStore.Save(profile,legacy);GameSaveStore.DeleteProgress(key,legacy);
                Require(!GameSaveStore.HasSave(key) && !RebirthSaveStore.HasSave(legacy),"Full reset removes both current and legacy progress so it cannot resurrect");}
            finally {PlayerPrefs.DeleteKey(key);PlayerPrefs.DeleteKey(legacy);PlayerPrefs.Save();}
            b.MarkSavedAt(1000);b.AccrueOffline(4600);b.Reset();
            Require(b.Stage==1 && b.Gold==0 && b.TotalGoldEarned==0 && b.TotalRelics==0 && b.RebirthPoints==0 && b.TotalRebirths==0 && b.LifetimeBestStage==0 && b.PendingOfflineGold==0 && b.SavedUtcSeconds==0 && !b.AutomaticGrowth.HasValue,"Full reset clears all permanent, run, reward and time data");
            Debug.Log("RIFTRAIL growth subdivision/reset checks passed: old save migration, all five stats, cost walls, caps, scoped deletion and fresh state.");
        }
        private static void CheckOfflineRewards()
        {
            var legacy=new BattleSimulation();Require(!legacy.AccrueOffline(1000) && legacy.PendingOfflineGold==0,"Old saves without time get an anchor rather than retroactive income");
            var b=new BattleSimulation();b.MarkSavedAt(1000);Require(!b.AccrueOffline(1059),"Short foreground switches do not create rewards");
            b=new BattleSimulation();b.MarkSavedAt(1000);var relics=b.TotalRelics;
            Require(b.AccrueOffline(4600) && b.PendingOfflineGold==22860 && b.PendingOfflineSeconds==3600,"One hour uses the known slime farming rate including respawn time");
            Require(b.Gold==0 && b.TotalGoldEarned==0 && b.Stage==1 && b.FarmKills==0 && b.TotalRelics==relics,"Unclaimed income never advances live combat or grants boss/relic rewards");
            var pending=b.PendingOfflineGold;Require(!b.AccrueOffline(4600) && b.PendingOfflineGold==pending,"Same time interval cannot be accrued twice");
            Require(GameSaveStore.TryDecode(GameSaveStore.Encode(b.ExportGame()),out var data),"Unclaimed reward save decodes");
            var restored=BattleSimulation.Restore(data);Require(restored.PendingOfflineGold==pending && restored.PendingOfflineSeconds==3600,"Unclaimed reward survives reload");
            restored.ToggleAutomaticGrowth(RebirthUpgrade.Attack);Require(restored.TryClaimOffline() && restored.PendingOfflineGold==0 && restored.PendingOfflineSeconds==0
                && restored.TotalGoldEarned==pending && restored.UpgradeLevel>0 && restored.Gold<pending,"Claim pays once and feeds the chosen automatic stat");
            var gold=restored.Gold;Require(!restored.TryClaimOffline() && restored.Gold==gold && !restored.AccrueOffline(4500) && restored.SavedUtcSeconds==4600,"Duplicate claim and clock rollback cannot mint gold");
            var cap=new BattleSimulation();cap.MarkSavedAt(1000);cap.AccrueOffline(73000);
            Require(cap.PendingOfflineSeconds==28800 && cap.PendingOfflineGold==182880,"Offline reward caps at eight hours");
            var capped=cap.PendingOfflineGold;cap.AccrueOffline(145000);Require(cap.PendingOfflineGold==capped && cap.PendingOfflineSeconds==28800,"Reopening without claiming cannot bypass the time cap");
            var large=b.ExportGame();large.pendingOfflineGold=large.pendingOfflineSeconds=0;large.gold=large.totalGold=long.MaxValue-100;large.savedUtcSeconds=1000;
            var nearLimit=BattleSimulation.Restore(large);nearLimit.AccrueOffline(4600);Require(nearLimit.PendingOfflineGold==100 && nearLimit.TryClaimOffline() && nearLimit.Gold==long.MaxValue,"Offline payouts respect integer headroom");
            Debug.Log("RIFTRAIL offline checks passed: elapsed time, old saves, known farming rate, eight-hour cap, pending persistence, one-time claim, auto spending, rollback and numeric limits.");
        }
        private static void CheckRunSaveAndAbandon()
        {
            var b=new BattleSimulation(new RebirthProgress{relicSeed=12345});b.TrySelectHero(HeroKind.Kunoichi);Reach(b,6);
            Until(b,()=>b.Gold>=5000);
            foreach(var kind in new[]{RebirthUpgrade.AttackSpeed,RebirthUpgrade.CritChance,RebirthUpgrade.CritDamage,RebirthUpgrade.Gold})b.TryBuyGrowth(kind);
            b.ToggleAutomaticGrowth(RebirthUpgrade.Gold);
            Until(b,()=>b.CanChallengeBoss);Require(b.TryChallengeBoss(),"Save scenario enters a boss");
            var gold=b.Gold;var points=b.RebirthPoints;var relics=b.TotalRelics;var automatic=b.AutomaticGrowth;
            var fighting=b.ExportGame();var restoredFight=BattleSimulation.Restore(fighting);
            Require(restoredFight.Stage==b.Stage && restoredFight.Gold==gold && restoredFight.Encounter==EncounterKind.Farming
                && restoredFight.AutoBossAttempted && restoredFight.AutomaticGrowth==automatic,"Closing during a boss restores the same stage as manual-retry farming");
            Require(b.TryAbandonBoss() && b.Encounter==EncounterKind.Farming && b.Gold==gold && b.RebirthPoints==points && b.TotalRelics==relics
                && b.AutomaticGrowth==automatic && !b.TryAbandonBoss(),"Abandoning keeps progress, grants no reward and cannot repeat outside a boss");
            var json=GameSaveStore.Encode(b.ExportGame());
            Require(GameSaveStore.TryDecode(json,out var decoded),"Full run save decodes");var restored=BattleSimulation.Restore(decoded);
            Require(GameSaveStore.Encode(restored.ExportGame())==json && restored.Hero==HeroKind.Kunoichi,"All run and permanent data restore exactly");
            for(var i=0;i<5;i++)Require(restored.GrowthLevel((RebirthUpgrade)i)==b.GrowthLevel((RebirthUpgrade)i),"All five growth levels persist");
            decoded.levels[0]=0;decoded.permanent.attackLevel=99;Require(restored.UpgradeLevel==b.UpgradeLevel && restored.PermanentAttackLevel==b.PermanentAttackLevel,"Run restore copies nested input data");
            restored.Advance(5);Require(restored.Encounter==EncounterKind.Farming && restored.Stage==b.Stage,"Restore after abandonment does not auto-retry the boss");
            var victory=new BattleSimulation(new RebirthProgress{attackLevel=100,relicSeed=42});Until(victory,()=>victory.CanChallengeBoss);victory.TryChallengeBoss();Until(victory,()=>victory.Phase==BattlePhase.Victory);
            var resumed=BattleSimulation.Restore(victory.ExportGame());
            Require(resumed.Stage==2 && resumed.BossesCleared==1 && resumed.Gold==victory.Gold && resumed.TotalRelics==victory.TotalRelics,
                "Saving in the boss exit animation checkpoints the rewarded next stage");
            var rolls=resumed.ExportPermanentProgress().relicRolls;resumed.Advance(1);
            Require(resumed.BossesCleared==1 && resumed.ExportPermanentProgress().relicRolls==rolls,"Loading a boss victory cannot duplicate its reward or relic roll");
            var large=b.ExportGame();large.automatic=-1;large.gold=(1L<<53)+123;large.totalGold=large.gold;
            Require(GameSaveStore.TryDecode(GameSaveStore.Encode(large),out decoded) && decoded.gold==large.gold,"Save keeps exact integer currency above floating-point precision");
            var bad=b.ExportGame();bad.gold=-1;Require(!GameSaveStore.TryDecode(JsonUtility.ToJson(bad),out _),"Invalid run data is rejected");
            var key="Riftrail.Test.Model."+Guid.NewGuid().ToString("N");
            try
            {
                GameSaveStore.Save(b.ExportGame(),key);Require(GameSaveStore.TryLoad(out decoded,key) && GameSaveStore.Encode(decoded)==json,"Isolated full local save reload");
                PlayerPrefs.SetString(key,"invalid");PlayerPrefs.Save();Require(!GameSaveStore.TryLoad(out _,key) && PlayerPrefs.GetString(key)=="invalid","Corrupt saves are preserved on failed reads");
            }
            finally {PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            Require(RebirthSaveStore.TryDecode("{\"version\":1,\"points\":2,\"totalEarned\":2,\"attackLevel\":1}",out var old)
                && new BattleSimulation(old).ExportGame().permanent.points==2,"Legacy permanent progress can migrate into a new run save");
            Debug.Log("RIFTRAIL full save and boss abandonment checks passed: exact values, aliases, legacy migration, corruption, boss exit, active-boss recovery and no duplicate rewards.");
        }
        private static void CheckNumbersAndOverkill()
        {
            Require(GameNumber.Format(999)=="999" && GameNumber.Format(1000)=="1K" && GameNumber.Format(1234567)=="1.23M"
                && GameNumber.Format(1000000000)=="1G" && GameNumber.Format(999999)=="1M" && GameNumber.Format(long.MaxValue)=="9.22E",
                "Shared number units cover boundaries and the integer limit");
            var e=new EnemyState("test",10,MonsterKind.Orc,1,0);
            Require(e.Hit(150)==10 && e.Health==0 && e.LastHitPower==150,"Overkill display keeps the full hit while HP loss is clamped");
            var slime=new EnemyState("test",3,MonsterKind.Slime,1,0);slime.Hit(999);
            Require(slime.LastHitPower==1,"Fixed-damage slime still reports one");
            var ghost=new EnemyState("test",10,MonsterKind.Ghost,1,0);ghost.Elapsed=1.5;ghost.Hit(999);
            Require(ghost.LastHitPower==0,"Missed hits still report zero");
            var b=new BattleSimulation(new RebirthProgress{attackLevel=100,relicSeed=42});BattleAttack hit=default;
            b.Attacked+=value=>hit=value;Until(b,()=>b.CanChallengeBoss);b.TryChallengeBoss();Until(b,()=>b.Phase==BattlePhase.Victory);
            Require(hit.KillingBlow && hit.DisplayDamage>=hit.Damage,"Combat events distinguish displayed power and applied HP loss");
        }
        private static void CheckAutomaticGrowth()
        {
            var b=new BattleSimulation();Until(b,()=>b.Gold>=300);
            var gold=b.Gold;var count=b.GrowthPurchaseCount(RebirthUpgrade.AttackSpeed,0,out var price);
            Require(b.ToggleAutomaticGrowth(RebirthUpgrade.AttackSpeed) && b.GrowthLevel(RebirthUpgrade.AttackSpeed)==count && b.Gold==gold-price,
                "Auto selection immediately spends affordable one-level prices");
            Require(b.ToggleAutomaticGrowth(RebirthUpgrade.CritDamage) && b.AutomaticGrowth==RebirthUpgrade.CritDamage,"Selecting another stat replaces the previous auto selection");
            Require(b.ToggleAutomaticGrowth(RebirthUpgrade.CritDamage) && !b.AutomaticGrowth.HasValue,"Clicking the selected automatic stat turns it off");
            b.ToggleAutomaticGrowth(RebirthUpgrade.Attack);var level=b.UpgradeLevel;
            Until(b,()=>b.UpgradeLevel>level);
            Require(!b.CanUpgrade,"Earned gold is spent without another user action");
            var snapshot=Snapshot(b);b.IsPaused=true;b.Advance(30);Require(Snapshot(b)==snapshot,"Pause suspends automatic spending and combat");b.IsPaused=false;
            b.ToggleAutomaticGrowth(RebirthUpgrade.Attack);var off=b.UpgradeLevel;b.Advance(10);Require(b.UpgradeLevel==off,"Disabled auto stat stops buying");
            Reach(b,6);b.ToggleAutomaticGrowth(RebirthUpgrade.Gold);Require(b.TryRebirth() && !b.AutomaticGrowth.HasValue,"Rebirth clears the run's automatic selection");
        }
        private static void CheckAutomaticBoss()
        {
            var b=new BattleSimulation(new RebirthProgress{relicSeed=42});b.TrySelectHero(HeroKind.Wuxia);
            Until(b,()=>b.FarmKills==5);b.Advance(.05);
            Require(b.Encounter==EncounterKind.Boss && b.AutoBossAttempted,"The fifth kill automatically starts the regional boss");
            Until(b,()=>b.Phase==BattlePhase.Defeat);b.Advance(1.5);
            Require(b.Encounter==EncounterKind.Farming && b.FarmKills==5 && b.AutoBossAttempted,"A failed auto boss returns to unlocked normal farming");
            var defeats=b.Defeats;var gold=b.Gold;b.Advance(90);
            Require(b.Encounter==EncounterKind.Farming && b.Defeats==defeats && b.Gold>gold,"A failed auto boss never loops into automatic retries");
            Until(b,()=>b.Phase==BattlePhase.Victory);
            Require(b.TryChallengeBoss(),"Manual retry remains available even in a farming death transition");
            Require(!b.TryChallengeBoss(),"Boss retries cannot restart an active boss");
        }
        private static void CheckRelics()
        {
            var bonus=new BattleSimulation(new RebirthProgress{relicAttack=10,relicSpeed=10,relicGold=10});
            Require(bonus.AttackPower==13 && Math.Abs(bonus.SpeedMultiplier-1.05)<1e-9 && Math.Abs(bonus.GoldMultiplier-1.30)<1e-9,"Relic copies add permanent attack/speed/gold multipliers");
            var b=new BattleSimulation(new RebirthProgress{relicSeed=12345}); var drops=0;var saves=0;
            b.RelicFound+=kind=>drops++;b.PermanentProgressChanged+=()=>saves++;
            Reach(b,21);
            Require(b.TotalRelics>0 && b.TotalRelics<20 && b.TotalRelics==drops && b.ExportPermanentProgress().relicRolls==20 && saves==20,
                "Each boss rolls once, includes misses, and persists roll state");
            var profile=b.ExportPermanentProgress();var total=b.TotalRelics;
            Require(b.TryRebirth() && b.TotalRelics==total,"Rebirth preserves accumulated relics");
            Require(RebirthSaveStore.TryDecode(RebirthSaveStore.Encode(profile),out var loaded)
                && loaded.relicAttack==profile.relicAttack && loaded.relicSpeed==profile.relicSpeed && loaded.relicGold==profile.relicGold && loaded.relicRolls==20 && loaded.relicSeed==12345,
                "Relic quantities and random progress round trip through save");
            var copy=new BattleSimulation(loaded);loaded.relicAttack=999;
            Require(copy.TotalRelics==total,"Relic restore does not alias its input");
            Reach(b,2);Reach(copy,2);
            Require(copy.TotalRelics==b.TotalRelics && copy.ExportPermanentProgress().relicRolls==21,
                "Loaded seed and roll position preserve the next random result");
            var cap=new BattleSimulation(new RebirthProgress{relicAttack=1000,relicSpeed=1000,relicGold=1000,attackLevel=100});
            Until(cap,()=>cap.CanChallengeBoss);cap.TryChallengeBoss();Until(cap,()=>cap.Phase==BattlePhase.Victory);
            Require(cap.TotalRelics==3000 && cap.ExportPermanentProgress().relicRolls==1,"Full collection is capped without failing boss rewards");
            Require(!RebirthSaveStore.TryDecode("{\"version\":1,\"relicAttack\":-1}",out _),"Invalid relic quantities rejected");
            Debug.Log("RIFTRAIL relic checks passed: chance, duplicates, caps, bonuses, one roll per boss, persistence and rebirth retention.");
        }
        private static void CheckBossDuringKillTransition()
        {
            var b=new BattleSimulation();Until(b,()=>b.FarmKills==5);b.Advance(.05);
            Until(b,()=>b.Phase==BattlePhase.Defeat);b.Advance(1.5);Until(b,()=>b.Phase==BattlePhase.Victory);
            Require(b.CanChallengeBoss,"A farming kill after boss failure does not disable manual retry");
            b.IsPaused=true;Require(!b.TryChallengeBoss(),"Paused retry is blocked");b.IsPaused=false;
            var gold=b.Gold;Require(b.TryChallengeBoss() && b.TransitionRemaining==0 && b.BossTimeRemaining==15 && b.Gold==gold,"Manual retry replaces respawn without duplicating rewards");
            b.Advance(.5);Require(b.Encounter==EncounterKind.Boss,"Old respawn cannot interrupt the retried boss");
        }
        private static void CheckBatchGrowth()
        {
            var b=new BattleSimulation();
            Require(!b.TryBuyGrowthBatch(RebirthUpgrade.Attack,10),"Unfunded batch is atomic");
            Until(b,()=>b.Gold>=5000);
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade)))
            {
                var count=b.GrowthPurchaseCount(kind,10,out var cost);
                var before=b.Gold; var level=b.GrowthLevel(kind);
                if(before>=cost) Require(b.TryBuyGrowthBatch(kind,10) && b.GrowthLevel(kind)==level+count && b.Gold==before-cost,"Ten-level batch spends exact sum");
                else Require(!b.TryBuyGrowthBatch(kind,10) && b.Gold==before && b.GrowthLevel(kind)==level,"Unaffordable fixed batch never partially spends");
            }
            Until(b,()=>b.Gold>=b.GrowthCost(RebirthUpgrade.Gold));
            var max=b.GrowthPurchaseCount(RebirthUpgrade.Gold,0,out var maxCost); var gold=b.Gold; var lv=b.GrowthLevel(RebirthUpgrade.Gold);
            Require(max>0 && b.TryBuyGrowthBatch(RebirthUpgrade.Gold,0) && b.Gold==gold-maxCost && b.GrowthLevel(RebirthUpgrade.Gold)==lv+max,"MAX buys exact affordable count");
            Require(!b.CanBuyGrowth(RebirthUpgrade.Gold),"MAX leaves less than next cost");
            b.IsPaused=true; Require(!b.TryBuyGrowthBatch(RebirthUpgrade.Attack,1),"Paused batches blocked");
            Require(!b.TryBuyGrowthBatch((RebirthUpgrade)99,10) && !b.TryBuyGrowthBatch(RebirthUpgrade.Attack,-1),"Invalid batch arguments blocked");
        }
        private static void Require(bool condition,string message)
        { if(!condition) throw new InvalidOperationException("Combat check failed: "+message); }
        private static void Until(BattleSimulation b,Func<bool> condition)
        {
            for(var i=0;i<40000 && !condition();i++) b.Advance(.05);
            Require(condition(),"Expected combat state reached");
        }
        private static void Step(BattleSimulation b,bool challenge=true)
        {
            if(b.CanUpgrade) b.TryUpgrade();
            if(challenge && b.CanChallengeBoss && b.HasBossPower) b.TryChallengeBoss();
            b.Advance(.05);
        }
        private static void Reach(BattleSimulation b,int stage)
        {
            for(var i=0;i<400000 && b.Stage<stage;i++) Step(b);
            Require(b.Stage==stage && b.Encounter==EncounterKind.Farming,"Earned progression reaches requested region");
        }
        private static void CheckExtraTraits()
        {
            foreach(var k in new[]{MonsterKind.Mushroom,MonsterKind.MushroomKing}) {
                var e=new EnemyState("test",100,k,100,0); Require(e.Hit(10)==10,"Awake mushroom takes base damage");
                e.Elapsed=2.5; Require(e.IsSleeping && e.Hit(10)==20,"Sleeping mushroom takes double damage");
                e.Elapsed=4; Require(!e.IsSleeping,"Mushroom wakes on the cycle boundary"); }
            foreach(var k in new[]{MonsterKind.Skeleton,MonsterKind.SkeletonKnight}) {
                var e=new EnemyState("test",100,k,100,0); e.Hit(200);
                Require(e.Revived && e.Health==35,"Skeleton revives once at 35 percent without a kill"); e.Hit(200);
                Require(e.Health==0,"Skeleton cannot revive twice"); }
            foreach(var k in new[]{MonsterKind.Dokebi,MonsterKind.DokebiKing}) {
                var e=new EnemyState("test",100,k,100,0); long coins=0;
                for(var j=0;j<20;j++) {e.Hit(1); coins+=e.DropHitGold();}
                Require(e.CoinDrops==5 && coins==25,"Per-hit dokebi gold has a five-drop cap"); }
            foreach(var k in new[]{MonsterKind.Snowball,MonsterKind.YetiChief}) {
                var a=new EnemyState("test",100,k,100,0); var b=new EnemyState("test",100,k,100,0);
                a.Hit(10); b.Hit(10,true);
                Require(a.Shield==40 && b.Shield==30 && a.Health==100 && b.Health==100,"Heavy hits break ice twice as quickly and shield protects HP");
                b.Hit(100,true); Require(b.Shield==0 && b.Health==100,"Shield break does not overflow into body damage");
                b.Hit(10); Require(b.Health==90,"Exposed body takes normal damage"); }
            foreach(var k in new[]{MonsterKind.Ghost,MonsterKind.GhostGentleman}) {
                var e=new EnemyState("test",100,k,100,0); e.Elapsed=1.5;
                Require(!e.IsExposed && e.Hit(100)==0 && e.Health==100,"Phased ghost avoids damage");
                e.Elapsed=3; Require(e.IsExposed && e.Hit(10)==10,"Ghost returns on a deterministic boundary"); }
            foreach(var k in new[]{MonsterKind.Wyvern,MonsterKind.TreasureDragon}) {
                var e=new EnemyState("test",100,k,100,0); e.Hit(70); Require(e.IsArmored,"Dragon armor starts at thirty percent");
                Require(e.Hit(20)==10 && e.Hit(20,false,true)==15,"Critical hits bypass half of the dragon's armor reduction"); }
            foreach(HeroKind hero in Enum.GetValues(typeof(HeroKind)))
            for(var stage=5;stage<=10;stage++) {
                var power=BattleBalance.RequiredBossPower(stage,hero,1.37,0);
                Require(BattleBalance.CanDefeatBoss(stage,hero,1.37,0,power),"Trait-aware readiness actually clears the boss");
                Require(power==1 || !BattleBalance.CanDefeatBoss(stage,hero,1.37,0,power-1),"Boss guidance finds a sufficient minimum without critical luck"); }
            Debug.Log("RIFTRAIL extra traits passed: sleep, one revival, capped hit gold, heavy shield breaking, ghost phases, dragon armor, all six bosses.");
        }
        private static void CheckStages()
        {
            var normals=new[]{MonsterKind.Slime,MonsterKind.Goblin,MonsterKind.Orc,MonsterKind.Golem,MonsterKind.Mushroom,MonsterKind.Skeleton,MonsterKind.Dokebi,MonsterKind.Snowball,MonsterKind.Ghost,MonsterKind.Wyvern};
            var bosses=new[]{MonsterKind.SlimeKing,MonsterKind.GoblinChief,MonsterKind.OrcChief,MonsterKind.AncientGolem,MonsterKind.MushroomKing,MonsterKind.SkeletonKnight,MonsterKind.DokebiKing,MonsterKind.YetiChief,MonsterKind.GhostGentleman,MonsterKind.TreasureDragon};
            var b=new BattleSimulation();
            for(var stage=1;stage<=20;stage++)
            {
                Reach(b,stage);
                Require(StageCatalog.Monster(stage)==normals[(stage-1)%10] && StageCatalog.Boss(stage)==bosses[(stage-1)%10],"Catalog defines matching families and repeats after ten bosses");
                Require(b.Enemy.Kind==StageCatalog.Monster(stage),"Stage starts with its assigned monster");
                for(var i=0;i<40000 && !b.CanChallengeBoss;i++)
                { Require(b.Enemy.Kind==StageCatalog.Monster(stage),"Farming never mixes monster families"); Step(b,false); }
                Require(b.FarmKills==5 && b.Stage==stage,"Five kills reveal the local boss without entering it");
                var snapshot=b.ExportPermanentProgress(); var gold=b.Gold;
                var expectedHealth=BattleBalance.BossHealth(stage,b.OrcRage);
                Require(b.TryChallengeBoss() && !b.TryChallengeBoss(),"Explicit boss challenge cannot repeat");
                Require(b.Enemy.Kind==StageCatalog.Boss(stage) && b.Enemy.MaxHealth==expectedHealth
                    && b.Enemy.GoldReward==BattleBalance.BossGold(stage),"Local boss has correct family, health and stage reward");
                while(b.CanUpgrade) b.TryUpgrade();
                for(var i=0;i<400000 && b.Stage==stage;i++) Step(b);
                Require(b.Stage==stage+1 && b.BossesCleared==stage && b.FarmKills==0 && b.OrcRage==0,"Only boss victory advances stage and clears local progress");
                Require(b.RunHighestClearedStage==stage && b.LifetimeBestStage==stage,"Regional bosses count toward rebirth records");
            }
            for(var stage=2;stage<=100;stage++) Require(BattleBalance.KillGold(stage)>BattleBalance.KillGold(stage-1),"Base stage gold grows monotonically");
            Require(BattleBalance.BossGold(5)>BattleBalance.BossGold(4) && BattleBalance.IsElite(5),"Fifth-stage elite retains a larger reward");
            var fail=new BattleSimulation(); Reach(fail,3); Until(fail,()=>fail.CanChallengeBoss);
            var rage=fail.OrcRage; var heldGold=fail.Gold; var level=fail.UpgradeLevel;
            Require(fail.TryChallengeBoss(),"Orc chief can be challenged");
            for(var i=0;i<400 && fail.Phase==BattlePhase.Fighting;i++)
            { fail.TrySelectHero(i%2==0?HeroKind.Wuxia:HeroKind.SwordMaster); fail.Advance(.05); }
            Require(fail.Phase==BattlePhase.Defeat,"Stalled boss fight times out"); fail.Advance(1.5);
            Require(fail.Stage==3 && fail.Enemy.Kind==MonsterKind.Orc && fail.OrcRage==rage && fail.Gold==heldGold && fail.UpgradeLevel==level,"Failure returns to the same family and preserves rage, gold and growth");
            b.Reset(); Require(b.Stage==1 && b.Enemy.Kind==MonsterKind.Slime,"Reset returns to the first region");
            Debug.Log("RIFTRAIL stage checks passed: twenty stages, fixed families, corresponding bosses, rewards, elite, failure, records, reset.");
        }
        private static void CheckTraits()
        {
            var b=new BattleSimulation(); var slimeHits=0;
            b.Attacked+=a=>{ if(b.Enemy.Kind==MonsterKind.Slime) { Require(a.Damage==1 && !a.IsCritical,"Normal slime damage stays exactly one"); slimeHits++; } };
            Until(b,()=>b.Phase==BattlePhase.Victory); Require(slimeHits==3 && b.Gold==15,"Slime retains low HP and one reward");
            var reward=b.Gold; b.Advance(.4); Require(b.Gold==reward,"Victory does not repeat rewards");
            Reach(b,2); Until(b,()=>b.Phase==BattlePhase.Fighting); var kills=b.FarmKills; reward=b.Gold;
            for(var i=0;i<100 && b.Phase==BattlePhase.Fighting;i++)
            { b.TrySelectHero(i%2==0?HeroKind.Wuxia:HeroKind.SwordMaster); b.Advance(.05); }
            Require(b.Phase==BattlePhase.Escaped && b.Gold==reward && b.FarmKills==kills,"Goblin escape gives neither gold nor boss discovery");
            b.TrySelectHero(HeroKind.Kunoichi); b.Advance(.5);
            Until(b,()=>b.Phase==BattlePhase.Victory);
            Require(b.LastKillWasBonus,"Fast goblin kills retain double-gold bonus");
            Reach(b,3); Until(b,()=>b.OrcRage==5);
            Require(b.OrcRage==5,"Five orc kills cap rage before automatic boss entry");
            b.Advance(.05);
            Require(b.Enemy.Kind==MonsterKind.OrcChief && b.Enemy.RageLevel==5 && b.Enemy.MaxHealth==BattleBalance.BossHealth(3,5),"Automatic orc chief inherits the capped rage snapshot");
            Require(BattleBalance.BossHealth(3,5)>BattleBalance.BossHealth(3,0),"Orc chief inherits rage health bonus");
            Reach(b,4);
            Require(b.Enemy.MaxHealth==BattleBalance.MonsterHealth(4)*3 && b.Enemy.GoldReward==BattleBalance.KillGold(4)*4 && b.KillReward==(long)Math.Round(b.Enemy.GoldReward*b.GoldMultiplier),"Golem region retains health and reward traits");
            Require(BattleBalance.StrikeDamage(10,HeroKind.Wuxia,MonsterKind.AncientGolem)==25
                && BattleBalance.StrikeDamage(10,HeroKind.Wuxia,MonsterKind.SlimeKing)==20,"Heavy strike breaks ancient golem armor");
            Until(b,()=>b.CanChallengeBoss); b.TryChallengeBoss(); var hp=b.Enemy.Health; b.Advance(.3);
            Require(b.Enemy.Health<=hp,"Regional boss is a real combat enemy");
        }
        private static string Snapshot(BattleSimulation b) => $"{b.Stage}|{b.BattleId}|{b.Enemy.Kind}|{b.Enemy.Health}|{b.Gold}|{b.BossesCleared}|{b.EnemyElapsed:F5}|{b.Phase}";
        private static void CheckHeroesAndTiming()
        {
            var ninja=new BattleSimulation(); ninja.TrySelectHero(HeroKind.Kunoichi); var hits=0;
            ninja.Attacked+=a=>hits++; ninja.Advance(.15); Require(hits==1 && ninja.Enemy.Health==2,"Ninja first strike");
            ninja.IsPaused=true; ninja.Advance(10); Require(hits==1,"Pause freezes pending follow-up"); ninja.IsPaused=false;
            ninja.Advance(.15); Require(hits==2 && ninja.Enemy.Health==1,"Ninja follow-up remains a separate hit");
            var swap=new BattleSimulation(); swap.TrySelectHero(HeroKind.Kunoichi); swap.Advance(.15); swap.TrySelectHero(HeroKind.Wuxia); swap.Advance(.15);
            Require(swap.Enemy.Health==2,"Changing heroes cancels pending ninja follow-up");
            foreach(HeroKind hero in Enum.GetValues(typeof(HeroKind)))
            {
                var meta=new RebirthProgress {speedLevel=100,critChanceLevel=100,critDamageLevel=100,relicSeed=42};
                var fast=new BattleSimulation(meta); Reach(fast,3); fast.TrySelectHero(hero);
                var slow=new BattleSimulation(meta); Reach(slow,3); slow.TrySelectHero(hero);
                var ft=new StringBuilder(); var st=new StringBuilder(); var crits=0;
                fast.Attacked+=a=>
                {
                    ft.Append(a.Damage).Append(a.IsCritical).Append(a.FollowUp);
                    if(a.IsCritical) crits++;
                    var raw=BattleBalance.StrikeDamage(fast.AttackPower,hero,fast.Enemy.Kind);
                    var expected=a.IsCritical?(long)Math.Round(raw*fast.CritMultiplier):raw;
                    Require((fast.Enemy.Kind!=MonsterKind.Orc && fast.Enemy.Kind!=MonsterKind.OrcChief) || a.Damage==Math.Min(expected,fast.Enemy.Health+a.Damage),"Per-hit critical multiplier affects actual damage");
                };
                slow.Attacked+=a=>st.Append(a.Damage).Append(a.IsCritical).Append(a.FollowUp);
                for(var i=0;i<3600;i++) fast.Advance(1d/60);
                for(var i=0;i<600;i++) slow.Advance(.1);
                Require(crits>0 && Snapshot(fast)==Snapshot(slow) && ft.ToString()==st.ToString(),"Critical hits and hero timing match at 60/10 FPS");
            }
            foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,-1})
            { var rejected=false; try {new BattleSimulation().Advance(invalid);} catch(ArgumentOutOfRangeException){rejected=true;} Require(rejected,"Invalid time rejected"); }
        }
        private static void CheckGrowthAndSave()
        {
            var b=new BattleSimulation(); Require(!b.TryRebirth() && !b.TryUpgrade() && !b.TryChallengeBoss(),"No free growth, rebirth or early boss");
            b.Advance(600);
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade)))
            { var gold=b.Gold; var cost=b.GrowthCost(kind); Require(b.TryBuyGrowth(kind) && b.Gold==gold-cost && b.GrowthLevel(kind)==1,"Five gold stats have exact prices"); }
            Require(b.AttackInterval<.8 && b.CritChance==.0005 && Math.Abs(b.CritMultiplier-1.505)<1e-9 && Math.Abs(b.GoldMultiplier-1.005)<1e-9,"Five stats affect actual values");
            b.IsPaused=true; var frozen=Snapshot(b); b.Advance(30);
            Require(Snapshot(b)==frozen && !b.TryBuyGrowth(RebirthUpgrade.Gold),"Pause freezes combat and gold purchases"); b.IsPaused=false;
            b.TrySelectHero(HeroKind.Wuxia); Reach(b,11);
            Require(b.RebirthReward==2,"Rebirth counts regional boss clears"); b.IsPaused=true;
            Require(b.TryRebirth() && b.RebirthPoints==2 && b.TotalRebirths==1 && b.Stage==1 && b.Gold==0 && b.Hero==HeroKind.Wuxia && b.IsPaused,"Rebirth rewards and reset/keep behavior");
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade))) Require(b.GrowthLevel(kind)==0,"All run stats reset");
            Require(!b.TryRebirth(),"Duplicate rebirth cannot claim points");
            Require(b.TryBuyPermanent(RebirthUpgrade.Attack) && b.TryBuyPermanent(RebirthUpgrade.Gold) && b.AttackPower==(long)Math.Round(11*(1+.03*b.RelicCount(RelicKind.Blade)))
                && b.KillReward==(long)Math.Round(b.Enemy.GoldReward*1.1*(1+.03*b.RelicCount(RelicKind.Coin))) && b.RebirthPoints==0,"Permanent stats apply and spend once");
            Require(!b.TryBuyPermanent(RebirthUpgrade.Gold) && !b.TryBuyPermanent((RebirthUpgrade)99),"Insufficient and invalid permanent purchases rejected");
            var profile=b.ExportPermanentProgress(); var restored=new BattleSimulation(profile); profile.attackLevel=99;
            Require(restored.AttackPower==b.AttackPower && restored.TotalRebirths==1 && restored.LifetimeBestStage==10 && restored.Stage==1,"Restore copies permanent profile without aliasing");
            var all=new BattleSimulation(new RebirthProgress {points=5,totalEarned=5});
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade))) Require(all.TryBuyPermanent(kind),"All five permanent stats purchase");
            Require(RebirthSaveStore.TryDecode(RebirthSaveStore.Encode(all.ExportPermanentProgress()),out var decoded)
                && decoded.speedLevel==1 && decoded.critChanceLevel==1 && decoded.critDamageLevel==1,"New saved stat fields round trip");
            Require(RebirthSaveStore.TryDecode("{\"version\":1,\"points\":2,\"totalEarned\":2,\"attackLevel\":1,\"goldLevel\":1}",out decoded)
                && decoded.speedLevel==0,"Old saves remain compatible");
            Require(!RebirthSaveStore.TryDecode("{\"version\":9}",out _) && !RebirthSaveStore.TryDecode("invalid",out _)
                && !RebirthSaveStore.TryDecode("{\"version\":1,\"speedLevel\":101}",out _),"Invalid saved data rejected");
            var cap=new BattleSimulation(new RebirthProgress {points=100,totalEarned=100,attackLevel=100,goldLevel=10});
            Require(!cap.TryBuyPermanent(RebirthUpgrade.Attack) && cap.PermanentUpgradeCost(RebirthUpgrade.Gold)==2
                && cap.TryBuyPermanent(RebirthUpgrade.Gold) && cap.RebirthPoints==98,"Caps and rising permanent costs");
            var key="Riftrail.Rebirth.Test."+Guid.NewGuid().ToString("N");
            try { RebirthSaveStore.Save(all.ExportPermanentProgress(),key); Require(RebirthSaveStore.TryLoad(out decoded,key) && decoded.critChanceLevel==1,"Isolated local save reload"); }
            finally {PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();}
        }
        private static void CheckProgression()
        {
            foreach(HeroKind hero in Enum.GetValues(typeof(HeroKind)))
            {
                var b=new BattleSimulation(); b.TrySelectHero(hero); double elapsed=0;
                while(b.Stage<=20 && elapsed<3600) { Step(b); elapsed+=.05; Require(b.Gold>=0,"Gold cannot go negative"); }
                Require(b.Stage==21 && b.BossesCleared==20 ,"Twenty stage families progress using earned gold for each hero");
                Debug.Log($"RIFTRAIL regional progression passed: {hero}, 20 stages {elapsed:F1}s, escapes {b.Escapes}.");
            }
            long previous=0;
            for(var level=0;level<=BattleBalance.MaxUpgradeLevel;level++)
            { Require(BattleBalance.Attack(level)>=previous && BattleBalance.Cost(level)>0,"Numeric tuning range"); previous=BattleBalance.Attack(level); }
        }
    }
}

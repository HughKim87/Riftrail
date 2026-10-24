using System;

namespace Riftrail
{
    /// <summary>Prototype tuning in one place. Integer limits are checked at the end of the tuning range.</summary>
    public static class BattleBalance
    {
        public const int GrowthSubdivision=10;
        public const int MaxUpgradeLevel=1800;
        public const int MaxGrowthLevel=1000;
        public const int GoldBarrierEvery=100;
        public const double GoldBarrierMultiplier=2;
        public const int StatStageLimit=100;
        public static bool IsElite(int stage)=>stage%5==0;
        private static int StageIndex(int stage)=>Math.Min(StatStageLimit,Math.Max(1,stage))-1;
        public static long LegacyAttack(int level)=>(long)Math.Round(10*Math.Pow(1.22,Math.Min(180,Math.Max(0,level))));
        public static double AttackExact(int level)
        {
            var scaled=Math.Min(MaxUpgradeLevel,Math.Max(0,level))/(double)GrowthSubdivision;var low=(int)scaled;
            return LegacyAttack(low)+(LegacyAttack(low+1)-LegacyAttack(low))*(scaled-low);
        }
        public static long Attack(int level)=>(long)Math.Round(AttackExact(level));
        public static double BarrierFactor(int level)=>Math.Pow(GoldBarrierMultiplier,Math.Max(0,level)/GoldBarrierEvery);
        private static long Price(double initial,double factor,int level)
        {
            var raw=initial/GrowthSubdivision*Math.Pow(factor,Math.Max(0,level)/(double)GrowthSubdivision)*BarrierFactor(level);
            return Math.Max(1,(long)Math.Min(long.MaxValue/4d,Math.Ceiling(raw)));
        }
        public static long Cost(int level)=>Price(20,1.24,Math.Min(MaxUpgradeLevel,Math.Max(0,level)));
        public static long GrowthCost(RebirthUpgrade kind,int level)
        {
            var initial=kind==RebirthUpgrade.AttackSpeed?50:kind==RebirthUpgrade.CritChance?75:kind==RebirthUpgrade.CritDamage?60:40;
            return Price(initial,1.18,Math.Min(MaxGrowthLevel,Math.Max(0,level)));
        }
        public static long MonsterHealth(int stage) => (long)Math.Round(30 * Math.Pow(1.27, StageIndex(stage)));
        public static long BossHealth(int stage, int rage = 0)
        {
            var kind=StageCatalog.Boss(stage);
            var family=kind==MonsterKind.AncientGolem ? 1.5 : kind==MonsterKind.GoblinChief ? .85
                : kind==MonsterKind.OrcChief ? 1+.15*Math.Min(5,Math.Max(0,rage)) : 1;
            return (long)Math.Round(320 * Math.Pow(1.32,StageIndex(stage)) * family * (IsElite(stage)?1.20:1));
        }
        public static long KillGold(int stage) => (long)Math.Round(15 * Math.Pow(1.26, StageIndex(stage)));
        public static long BossGold(int stage) => (long)Math.Round(60 * Math.Pow(1.26, StageIndex(stage)) * (IsElite(stage) ? 2 : 1));
        public static double HeroInterval(HeroKind hero) => hero == HeroKind.Wuxia ? 1.6 : hero == HeroKind.Kunoichi ? .65 : .8;
        public static double FirstAttack(HeroKind hero) => hero == HeroKind.Wuxia ? .55 : hero == HeroKind.Kunoichi ? .15 : .3;
        public static long StrikeDamage(long attack, HeroKind hero, MonsterKind target)
        {
            var multiplier = hero == HeroKind.Wuxia ? ((target == MonsterKind.Golem || target == MonsterKind.AncientGolem) ? 2.5 : 2) : hero == HeroKind.Kunoichi ? .45 : 1;
            return Math.Max(1, (long)Math.Round(attack * multiplier));
        }
        public static long Health(int stage, MonsterKind kind, int rage = 0)
        {
            var normal = MonsterHealth(stage);
            switch (kind)
            {
                case MonsterKind.Slime: return 3 + Math.Min(2, (Math.Max(1, stage)-1)/10);
                case MonsterKind.Golem: return normal * 3;
                case MonsterKind.Orc: return (long)Math.Round(normal * (1 + .35 * Math.Min(5, Math.Max(0, rage))));
                case MonsterKind.Goblin: return Math.Max(1, (long)Math.Round(normal * .7));
                case MonsterKind.Mushroom: return (long)Math.Round(normal*1.1);
                case MonsterKind.Skeleton: return Math.Max(1,(long)Math.Round(normal*.75));
                case MonsterKind.Dokebi: return normal;
                case MonsterKind.Snowball: return (long)Math.Round(normal*1.1);
                case MonsterKind.Ghost: return Math.Max(1,(long)Math.Round(normal*.9));
                case MonsterKind.Wyvern: return (long)Math.Round(normal*1.3);
                default: return BossHealth(stage, rage);
            }
        }
        public static long Reward(int stage, MonsterKind kind, int rage = 0)
        {
            var normal = KillGold(stage);
            switch (kind)
            {
                case MonsterKind.Golem: return normal * 4;
                case MonsterKind.Orc: return (long)Math.Round(normal * (1 + .30 * Math.Min(5, Math.Max(0, rage))));
                case MonsterKind.Guardian: case MonsterKind.SlimeKing: case MonsterKind.GoblinChief:
                case MonsterKind.OrcChief: case MonsterKind.AncientGolem:
                case MonsterKind.MushroomKing: case MonsterKind.SkeletonKnight: case MonsterKind.DokebiKing:
                case MonsterKind.YetiChief: case MonsterKind.GhostGentleman: case MonsterKind.TreasureDragon: return BossGold(stage);
                default: return normal;
            }
        }
        public static long RequiredBossPower(int stage, HeroKind hero, double speed = 1, int rage = 0)
        {
            // Simulate the real trait rules without critical luck; the model caches this guidance until its inputs change.
            long low=1, high=BossHealth(stage,rage)*8;
            while(low<high)
            {
                var middle=low+(high-low)/2;
                if(CanDefeatBoss(stage,hero,speed,rage,middle)) high=middle; else low=middle+1;
            }
            return low;
        }
        internal static bool CanDefeatBoss(int stage,HeroKind hero,double speed,int rage,long power)
        {
            var kind=StageCatalog.Boss(stage);
            var enemy=new EnemyState(StageCatalog.Name(kind),BossHealth(stage,rage),kind,BossGold(stage),rage);
            var cooldown=FirstAttack(hero)/speed; var follow=-1d;
            for(var tick=1;tick<=300;tick++)
            {
                enemy.Elapsed=tick*BattleSimulation.StepSeconds; cooldown-=BattleSimulation.StepSeconds;
                if(follow>=0) { follow-=BattleSimulation.StepSeconds; if(follow<=1e-9) { follow=-1;
                    enemy.Hit(StrikeDamage(power,hero,kind),hero==HeroKind.Wuxia); if(enemy.Health==0) return true; } }
                if(cooldown<=1e-9) { cooldown+=HeroInterval(hero)/speed;
                    enemy.Hit(StrikeDamage(power,hero,kind),hero==HeroKind.Wuxia); if(enemy.Health==0) return true;
                    if(hero==HeroKind.Kunoichi) follow=.12/speed; }
            }
            return false;
        }
    }
}

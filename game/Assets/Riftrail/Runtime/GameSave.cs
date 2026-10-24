using System;
namespace Riftrail
{
    [Serializable]
    public sealed class GameSave
    {
        public int version=2;
        public RebirthProgress permanent;
        public int stage, hero, farmKills, rage, highestCleared, bossesCleared, defeats, escapes;
        public long gold, totalGold, criticalState;
        public int[] levels;
        public int automatic=-1;
        public bool bossAttempted;
        public long savedUtcSeconds, pendingOfflineGold, pendingOfflineSeconds;
        public GameSave ToCurrent()
        {
            if(version!=1)return this;
            var upgraded=(GameSave)MemberwiseClone();upgraded.permanent=permanent.Copy();upgraded.levels=(int[])levels.Clone();
            for(var i=0;i<5;i++)upgraded.levels[i]*=BattleBalance.GrowthSubdivision;
            upgraded.version=2;return upgraded;
        }
        public bool IsValid
        {
            get
            {
                if(savedUtcSeconds<0 || savedUtcSeconds>253402300799L || pendingOfflineGold<0 || pendingOfflineSeconds<0 || pendingOfflineSeconds>28800
                    || (pendingOfflineGold==0)!=(pendingOfflineSeconds==0) || pendingOfflineGold>long.MaxValue-totalGold
                    || (version!=1 && version!=2) || permanent==null || !permanent.IsValid || stage<1 || stage>=int.MaxValue
                    || !Enum.IsDefined(typeof(HeroKind),hero) || farmKills<0 || farmKills>5 || rage<0 || rage>5
                    || highestCleared!=stage-1 || bossesCleared!=highestCleared || permanent.bestClearedStage<highestCleared
                    || defeats<0 || escapes<0 || gold<0 || totalGold<gold || criticalState<=0 || criticalState>uint.MaxValue
                    || levels==null || levels.Length!=5 || automatic< -1 || automatic>4 || (bossAttempted && farmKills!=5))return false;
                for(var i=0;i<5;i++)if(levels[i]<0 || levels[i]>(i==0?(version==1?180:BattleBalance.MaxUpgradeLevel):(version==1?100:BattleBalance.MaxGrowthLevel)))return false;
                return automatic<0 || levels[automatic]<(automatic==0?(version==1?180:BattleBalance.MaxUpgradeLevel):(version==1?100:BattleBalance.MaxGrowthLevel));
            }
        }
    }
}

using System;

namespace Riftrail
{
    public enum RebirthUpgrade { Attack, Gold, AttackSpeed, CritChance, CritDamage }

    [Serializable]
    public sealed class RebirthProgress
    {
        public int version = 1;
        public long points;
        public long totalEarned;
        public int attackLevel;
        public int goldLevel;
        public int speedLevel, critChanceLevel, critDamageLevel;
        public int rebirths;
        public int bestClearedStage;
        public int relicAttack, relicSpeed, relicGold, relicRolls, relicSeed;
        public const int MaxLevel = 100;
        public bool IsValid => version == 1 && points >= 0 && totalEarned >= points && attackLevel >= 0
            && attackLevel <= MaxLevel && goldLevel >= 0 && goldLevel <= MaxLevel && speedLevel >= 0 && speedLevel <= MaxLevel && critChanceLevel >= 0 && critChanceLevel <= MaxLevel
            && critDamageLevel >= 0 && critDamageLevel <= MaxLevel && rebirths >= 0 && bestClearedStage >= 0 && relicAttack>=0 && relicAttack<=1000
            && relicSpeed>=0 && relicSpeed<=1000 && relicGold>=0 && relicGold<=1000 && relicRolls>=0 && relicSeed>=0;
        public RebirthProgress Copy() => new RebirthProgress
        {
            version = version, points = points, totalEarned = totalEarned, attackLevel = attackLevel,
            goldLevel = goldLevel, speedLevel = speedLevel, critChanceLevel = critChanceLevel, critDamageLevel = critDamageLevel, rebirths = rebirths, bestClearedStage = bestClearedStage,
            relicAttack=relicAttack, relicSpeed=relicSpeed, relicGold=relicGold, relicRolls=relicRolls, relicSeed=relicSeed
        };
        public int Level(RebirthUpgrade kind)
        {
            switch(kind) {
                case RebirthUpgrade.Attack: return attackLevel;
                case RebirthUpgrade.Gold: return goldLevel;
                case RebirthUpgrade.AttackSpeed: return speedLevel;
                case RebirthUpgrade.CritChance: return critChanceLevel;
                case RebirthUpgrade.CritDamage: return critDamageLevel;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        public void Increase(RebirthUpgrade kind)
        {
            switch(kind) {
                case RebirthUpgrade.Attack: attackLevel++; break;
                case RebirthUpgrade.Gold: goldLevel++; break;
                case RebirthUpgrade.AttackSpeed: speedLevel++; break;
                case RebirthUpgrade.CritChance: critChanceLevel++; break;
                case RebirthUpgrade.CritDamage: critDamageLevel++; break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        public static int Cost(int level) => 1 + level / 10;
        public static long Apply(long value, int level) => (long)Math.Round(value * (1 + .10 * level));
    }
}

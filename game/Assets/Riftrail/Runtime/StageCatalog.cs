using System;
namespace Riftrail
{
    public static class StageCatalog
    {
        public const int RegionCount=10;
        private static readonly MonsterKind[] Monsters={MonsterKind.Slime,MonsterKind.Goblin,MonsterKind.Orc,MonsterKind.Golem,
            MonsterKind.Mushroom,MonsterKind.Skeleton,MonsterKind.Dokebi,MonsterKind.Snowball,MonsterKind.Ghost,MonsterKind.Wyvern};
        private static readonly MonsterKind[] Bosses={MonsterKind.SlimeKing,MonsterKind.GoblinChief,MonsterKind.OrcChief,MonsterKind.AncientGolem,
            MonsterKind.MushroomKing,MonsterKind.SkeletonKnight,MonsterKind.DokebiKing,MonsterKind.YetiChief,MonsterKind.GhostGentleman,MonsterKind.TreasureDragon};
        private static readonly string[] Regions={"슬라임 초원","고블린 야영지","오크 요새","수정 유적","버섯 숲","뼈의 묘지","도깨비 고개","얼음 동굴","유령 저택","용의 둥지"};
        private static readonly string[] Names={"초원 슬라임","수정 골렘","오크 전사","야영지 고블린","이끼 수호자","슬라임 왕","고블린 두목","오크 족장","고대 골렘","졸린 버섯","꼬마 해골","꼬마 도깨비","눈뭉치","꼬마 유령","아기 와이번","버섯왕","해골 기사","도깨비 대왕","설인 족장","유령 신사","보물룡"};
        public static int RegionIndex(int stage)=>(Math.Max(1,stage)-1)%RegionCount;
        public static MonsterKind Monster(int stage)=>Monsters[RegionIndex(stage)];
        public static MonsterKind Boss(int stage)=>Bosses[RegionIndex(stage)];
        public static string Region(int stage)=>Regions[RegionIndex(stage)];
        public static string Name(MonsterKind kind)=>Names[(int)kind];
        public static bool IsMushroom(MonsterKind k)=>k==MonsterKind.Mushroom||k==MonsterKind.MushroomKing;
        public static bool IsSkeleton(MonsterKind k)=>k==MonsterKind.Skeleton||k==MonsterKind.SkeletonKnight;
        public static bool IsDokebi(MonsterKind k)=>k==MonsterKind.Dokebi||k==MonsterKind.DokebiKing;
        public static bool IsIce(MonsterKind k)=>k==MonsterKind.Snowball||k==MonsterKind.YetiChief;
        public static bool IsGhost(MonsterKind k)=>k==MonsterKind.Ghost||k==MonsterKind.GhostGentleman;
        public static bool IsDragon(MonsterKind k)=>k==MonsterKind.Wyvern||k==MonsterKind.TreasureDragon;
    }
}

namespace Riftrail
{
    public enum RelicKind { Blade, Feather, Coin }
    public static class RelicCatalog
    {
        public static string Name(RelicKind kind) => kind==RelicKind.Blade?"고대 검의 조각":kind==RelicKind.Feather?"바람의 깃털":"행운의 동전";
        public static string Effect(RelicKind kind,int count) => kind==RelicKind.Blade?$"공격력 +{GameNumber.Format(count*3)}%":kind==RelicKind.Feather?$"공격속도 +{count*.5:0.#}%":$"골드 획득 +{GameNumber.Format(count*3)}%";
    }
}

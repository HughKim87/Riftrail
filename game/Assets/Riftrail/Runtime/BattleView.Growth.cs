using UnityEngine;
using UnityEngine.UI;

namespace Riftrail
{
    public sealed partial class BattleView
    {
        private static readonly RebirthUpgrade[] GrowthOrder = { RebirthUpgrade.Attack, RebirthUpgrade.AttackSpeed,
            RebirthUpgrade.CritChance, RebirthUpgrade.CritDamage, RebirthUpgrade.Gold };
        private readonly Text[] growthLabels=new Text[5], growthCosts=new Text[5],growthHints=new Text[5];
        private readonly Button[] growthButtons=new Button[5];
        private readonly Button[] quantityButtons=new Button[3];
        private readonly Button[] automaticButtons=new Button[5];
        private readonly Text[] automaticLabels=new Text[5];
        private int purchaseAmount=1;
        public bool IsGrowthMenuOpen => IsRebirthOpen || IsSettingsOpen || IsRelicsOpen || IsOfflineRewardOpen;
        private static string StatName(RebirthUpgrade kind)
        {
            switch(kind) {
                case RebirthUpgrade.Attack: return "공격력";
                case RebirthUpgrade.AttackSpeed: return "공격속도";
                case RebirthUpgrade.CritChance: return "치명타 확률";
                case RebirthUpgrade.CritDamage: return "치명타 피해";
                default: return "골드 획득";
            }
        }
        private static string PermanentValue(RebirthUpgrade kind,int level)
        {
            var amount=kind==RebirthUpgrade.AttackSpeed ? level*2d : kind==RebirthUpgrade.CritChance ? level*.25 : kind==RebirthUpgrade.CritDamage ? level*5d : level*10d;
            return $"+{amount:0.##}%{(kind==RebirthUpgrade.CritChance || kind==RebirthUpgrade.CritDamage?"포인트":"")}";
        }
        private static string GrowthValue(BattleSimulation s,RebirthUpgrade kind,int level)
        {
            switch(kind) {
                case RebirthUpgrade.Attack: return GameNumber.Precise(s.AttackExactAtLevel(level));
                case RebirthUpgrade.AttackSpeed: return $"{(1+.002*level)*(1+.02*s.PermanentLevel(kind))*(1+.005*s.RelicCount(RelicKind.Feather)):0.000}배";
                case RebirthUpgrade.CritChance: return $"{System.Math.Min(75,.05*level+.25*s.PermanentLevel(kind)):0.##}%";
                case RebirthUpgrade.CritDamage: return $"{GameNumber.Precise(150+.5*level+5*s.PermanentLevel(kind))}%";
                default: return $"{(1+.005*level)*(1+.10*s.PermanentGoldLevel)*(1+.03*s.RelicCount(RelicKind.Coin)):0.000}배";
            }
        }
        private void BuildGrowth()
        {
            var root=Panel(contentRoot,"PersistentGrowth",Cream).rectTransform;
            Anchor(root,.5f,.2708333f,1000,1000);
            Label(root,"성장",28,Muted,.13f,.94f,200,65);
            goldText=Label(root,"",34,Green,.39f,.94f,260,65);
            var amounts=new[]{1,10,0};
            for(var i=0;i<3;i++)
            {
                var amount=amounts[i];
                var label=MakeButton(root,"Quantity"+(amount==0?"Max":amount.ToString()),.61f+i*.14f,.94f,125,72,Green,()=>
                { CancelUpgradeHold(); purchaseAmount=amount; Render(simulation,false); },out quantityButtons[i]);
                label.text=amount==0?"최대":amount+"회"; label.fontSize=28;
            }
            for(var i=0;i<5;i++)
            {
                var kind=GrowthOrder[i];
                var row=Rect(root,"GrowthRow"+kind); Anchor(row,.5f,.80f-i*.165f,940,148);
                var separator=Panel(row,"Divider",Hex(0xDEDCCB)); Anchor(separator.rectTransform,.5f,0,940,2);
                growthLabels[i]=Label(row,"",32,Ink,.245f,.64f,430,100);
                growthLabels[i].alignment=TextAnchor.MiddleLeft;
                growthHints[i]=Label(row,"",22,Muted,.245f,.16f,430,36);growthHints[i].alignment=TextAnchor.MiddleLeft;
                growthCosts[i]=MakeButton(row,i==0?"UpgradeButton":"GrowthBuy"+kind,.64f,.53f,280,120,Green,()=>BuyGrowth(kind),out growthButtons[i]);
                growthCosts[i].fontSize=28;
                automaticLabels[i]=MakeButton(row,"AutoGrowth"+kind,.91f,.53f,140,120,Hex(0xDADDCF),()=>
                {
                    if(IsChoosingHero || IsGrowthMenuOpen)return;
                    CancelUpgradeHold();simulation.ToggleAutomaticGrowth(kind);Render(simulation,false);
                },out automaticButtons[i]);
                automaticLabels[i].fontSize=27;
            }
            Label(root,"자동 강화는 한 항목만 · 1레벨씩 구매 · 환생 시 해제",24,Muted,.5f,.027f,960,36);
        }
        public void BuyGrowth(RebirthUpgrade kind)
        {
            if(IsChoosingHero || IsGrowthMenuOpen) return;
            simulation.TryBuyGrowthBatch(kind,purchaseAmount);
            Render(simulation,false);
        }
        private void RenderGrowth(BattleSimulation s)
        {
            for(var i=0;i<3;i++)
                ((Image)quantityButtons[i].targetGraphic).color=purchaseAmount==new[]{1,10,0}[i]?Green:Hex(0x9AA697);
            for(var i=0;i<5;i++)
            {
                var kind=GrowthOrder[i]; var level=s.GrowthLevel(kind); var max=level>=s.GrowthLimit(kind);
                var count=s.GrowthPurchaseCount(kind,purchaseAmount,out var cost);
                var next=level+System.Math.Max(1,count);
                growthLabels[i].text=$"{StatName(kind)}   레벨 {level}\n{GrowthValue(s,kind,level)} {(max?"(최대)":$"-> {GrowthValue(s,kind,next)}")}";
                growthHints[i].text=max?"강화 완료":$"구간 비용 {Number((long)BattleBalance.BarrierFactor(level))}배 · 다음 {Number((level/100+1)*100)}";
                growthCosts[i].text=max?"최대":$"+{System.Math.Max(1,count)} 강화\n{Number(cost)} 골드";
                var selected=s.AutomaticGrowth==kind;
                automaticLabels[i].text=selected?"자동\n켜짐":"자동";
                automaticLabels[i].color=selected?Ink:Muted;
                ((Image)automaticButtons[i].targetGraphic).color=selected?Gold:Hex(0xDADDCF);
                automaticButtons[i].interactable=!s.IsPaused && !IsChoosingHero && !IsGrowthMenuOpen && (!max || selected);
                growthButtons[i].interactable=!s.IsPaused && count>0 && s.Gold>=cost && !IsChoosingHero && !IsGrowthMenuOpen;
            }
        }
    }
}

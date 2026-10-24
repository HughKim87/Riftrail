using UnityEngine;
using UnityEngine.UI;

namespace Riftrail
{
    public sealed partial class BattleView
    {
        private static readonly RebirthUpgrade[] GrowthOrder = { RebirthUpgrade.Attack, RebirthUpgrade.AttackSpeed,
            RebirthUpgrade.CritChance, RebirthUpgrade.CritDamage, RebirthUpgrade.Gold };
        private readonly Text[] growthLabels=new Text[5], growthCosts=new Text[5],growthValues=new Text[5];
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
        private static double GrowthValueRaw(BattleSimulation s,RebirthUpgrade kind,int level)
        {
            switch(kind) {
                case RebirthUpgrade.Attack: return s.AttackExactAtLevel(level);
                case RebirthUpgrade.AttackSpeed: return (1+.002*level)*(1+.02*s.PermanentLevel(kind))*(1+.005*s.RelicCount(RelicKind.Feather));
                case RebirthUpgrade.CritChance: return System.Math.Min(75,.05*level+.25*s.PermanentLevel(kind));
                case RebirthUpgrade.CritDamage: return 150+.5*level+5*s.PermanentLevel(kind);
                default: return (1+.005*level)*(1+.10*s.PermanentGoldLevel)*(1+.03*s.RelicCount(RelicKind.Coin));
            }
        }
        private static string GrowthValueText(RebirthUpgrade kind,double value,bool gain=false)
        {
            if(kind==RebirthUpgrade.AttackSpeed || kind==RebirthUpgrade.Gold)
                return value.ToString(gain?"0.###":"0.000",System.Globalization.CultureInfo.InvariantCulture)+"배";
            return GameNumber.Precise(value)+(kind==RebirthUpgrade.Attack?"":gain?"%p":"%");
        }
        private void BuildGrowth()
        {
            var root=Panel(contentRoot,"PersistentGrowth",Cream).rectTransform;
            Anchor(root,.5f,.2708333f,1080,1040);
            var coin=Oval(root,"WalletCoin",Gold,74,74);walletIcon=coin.rectTransform;Anchor(coin.rectTransform,.065f,.94f,74,74);
            OvalAt(coin.rectTransform,"CoinInset",Hex(0xBD8D32),0,0,52,52);
            OvalAt(coin.rectTransform,"CoinShine",Gold,0,0,38,38);
            goldText=Label(root,"",54,Ink,.28f,.94f,360,100);goldText.alignment=TextAnchor.MiddleLeft;goldText.fontStyle=FontStyle.Bold;
            var amounts=new[]{1,10,0};
            for(var i=0;i<3;i++)
            {
                var amount=amounts[i];
                var label=MakeButton(root,"Quantity"+(amount==0?"Max":amount.ToString()),.61f+i*.145f,.94f,140,90,Green,()=>
                { CancelUpgradeHold(); purchaseAmount=amount; Render(simulation,false); },out quantityButtons[i]);
                label.text=amount==0?"최대":amount+"회"; label.fontSize=38;
            }
            for(var i=0;i<5;i++)
            {
                var kind=GrowthOrder[i];
                var row=Rect(root,"GrowthRow"+kind); Anchor(row,.5f,.81f-i*.18f,1040,176);
                var separator=Panel(row,"Divider",Hex(0xDEDCCB)); Anchor(separator.rectTransform,.5f,0,1040,2);
                growthLabels[i]=Label(row,"",42,Ink,.245f,.74f,470,65);
                growthLabels[i].alignment=TextAnchor.MiddleLeft;growthLabels[i].fontStyle=FontStyle.Bold;
                growthValues[i]=Label(row,"",48,Ink,.245f,.34f,470,76);
                growthValues[i].alignment=TextAnchor.MiddleLeft;growthValues[i].fontStyle=FontStyle.Bold;
                growthValues[i].horizontalOverflow=HorizontalWrapMode.Overflow;
                growthCosts[i]=MakeButton(row,i==0?"UpgradeButton":"GrowthBuy"+kind,.635f,.52f,300,146,Green,()=>BuyGrowth(kind),out growthButtons[i]);
                growthCosts[i].fontSize=42;
                automaticLabels[i]=MakeButton(row,"AutoGrowth"+kind,.90f,.52f,180,146,Hex(0xDADDCF),()=>
                {
                    if(IsChoosingHero || IsGrowthMenuOpen)return;
                    CancelUpgradeHold();simulation.ToggleAutomaticGrowth(kind);Render(simulation,false);
                },out automaticButtons[i]);
                automaticLabels[i].fontSize=38;
            }
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
                var current=GrowthValueRaw(s,kind,level);
                var gain=System.Math.Max(0,GrowthValueRaw(s,kind,level+count)-current);
                growthLabels[i].text=$"{StatName(kind)}  <size=32>{level}강</size>";
                growthValues[i].text=GrowthValueText(kind,current)+(max?" <size=30>(최대)</size>":$" <color=#187653><size=36>(+{GrowthValueText(kind,gain,true)})</size></color>");
                growthCosts[i].text=max?"강화 완료":count==0?$"골드 부족\n{Number(cost)} 골드":$"+{count} 강화\n{Number(cost)} 골드";
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

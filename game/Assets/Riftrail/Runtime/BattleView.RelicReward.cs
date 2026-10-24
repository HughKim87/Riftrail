using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Riftrail
{
    public sealed partial class BattleView
    {
        private struct RelicRewardData { public RelicKind Kind;public int Count;public double Before,After; }
        private readonly Queue<RelicRewardData> relicRewards=new Queue<RelicRewardData>();
        private RectTransform rewardRoot, rewardRays, rewardCard;
        private CanvasGroup rewardAlpha;
        private Text rewardHeading, rewardName, rewardCount, rewardBonus, rewardActual, rewardIncrease;
        private readonly RectTransform[] rewardIcons=new RectTransform[3];
        private bool rewardShowing;
        private float rewardAge;
        private void BuildRelicReward()
        {
            rewardRoot=Rect(contentRoot,"RelicReward");Anchor(rewardRoot,.5f,.755f,980,660);
            rewardAlpha=rewardRoot.gameObject.AddComponent<CanvasGroup>();rewardAlpha.blocksRaycasts=false;rewardAlpha.interactable=false;
            rewardRays=Rect(rewardRoot,"RelicRays");Stretch(rewardRays);
            for(var i=0;i<12;i++) {var ray=Box(rewardRays,"Ray",new Color(.97f,.78f,.32f,.22f),0,0,14,630);ray.rectTransform.localRotation=Quaternion.Euler(0,0,i*15);}
            var glow=Oval(rewardRoot,"RelicGlow",new Color(1,.83f,.4f,.15f),750,630);
            rewardCard=Panel(rewardRoot,"RelicRewardCard",Cream).rectTransform;Anchor(rewardCard,.5f,.5f,920,520);
            var edge=Panel(rewardCard,"GoldEdge",Gold);Anchor(edge.rectTransform,.5f,1,920,8);
            rewardHeading=Label(rewardCard,"",30,Green,.5f,.9f,880,55);
            for(var i=0;i<3;i++)
            {
                var icon=Rect(rewardCard,"RewardIcon"+i);Anchor(icon,.14f,.66f,125,125);rewardIcons[i]=icon;
                OvalAt(icon,"Disc",i==0?Hex(0xC5906D):i==1?Hex(0x90BEAB):Gold,0,0,120,120);
                if(i==0) {var blade=Box(icon,"Blade",Cream,0,5,19,80);blade.rectTransform.localRotation=Quaternion.Euler(0,0,-25);}
                else if(i==1) OvalAt(icon,"Feather",Cream,0,0,30,85);
                else OvalAt(icon,"Coin",Hex(0xBC8A39),0,0,50,50);
            }
            rewardName=Label(rewardCard,"",40,Ink,.62f,.71f,660,70);
            rewardCount=Label(rewardCard,"",29,Muted,.62f,.58f,660,55);
            rewardBonus=Label(rewardCard,"",33,Green,.5f,.43f,880,65);
            rewardActual=Label(rewardCard,"",27,Ink,.5f,.28f,880,55);
            rewardIncrease=Label(rewardCard,"",25,Muted,.5f,.12f,880,55);
            rewardRoot.gameObject.SetActive(false);
        }
        public int RelicRewardsReceived {get;private set;}
        public void ShowRelicReward(RelicKind kind) => ShowRelicReward(kind,simulation.RelicCount(kind),simulation.LastRelicBefore,simulation.LastRelicAfter);
        internal void ShowRelicReward(RelicKind kind,int count,double before,double after)
        {
            RelicRewardsReceived++;
            relicRewards.Enqueue(new RelicRewardData{Kind=kind,Count=count,Before=before,After=after});
            if(!rewardShowing)NextRelicReward();
        }
        internal void ClearRelicRewards() { relicRewards.Clear();rewardShowing=false;rewardRoot.gameObject.SetActive(false); }
        private void NextRelicReward()
        {
            if(relicRewards.Count==0) {rewardShowing=false;rewardRoot.gameObject.SetActive(false);return;}
            var data=relicRewards.Dequeue();rewardShowing=true;rewardAge=0;
            rewardRoot.SetAsLastSibling();rewardRoot.gameObject.SetActive(true);
            rewardHeading.text=data.Count==1?"새로운 유물 발견!":"중복 유물 · 영구 효과 강화!";
            rewardName.text=RelicCatalog.Name(data.Kind);
            rewardCount.text=data.Count==1?"첫 발견 · 보유 1개":$"보유 {Number(data.Count-1)}개 → {Number(data.Count)}개";
            rewardBonus.text=$"{RelicCatalog.Effect(data.Kind,data.Count-1)} → {RelicCatalog.Effect(data.Kind,data.Count)}";
            var label=data.Kind==RelicKind.Blade?"최종 공격력":data.Kind==RelicKind.Feather?"총 공격속도":"총 골드 배율";
            var before=data.Kind==RelicKind.Blade?Number((long)data.Before):$"{data.Before:0.000}배";
            var after=data.Kind==RelicKind.Blade?Number((long)data.After):$"{data.After:0.000}배";
            var percent=data.Before>0?(data.After/data.Before-1)*100:0;
            rewardActual.text=$"{label} {before} → {after} (+{percent:0.##}%)";
            rewardIncrease.text=$"이번 획득: 보너스 +{(data.Kind==RelicKind.Feather?"0.5":"3")}%포인트 · 환생 후 유지";
            for(var i=0;i<3;i++)rewardIcons[i].gameObject.SetActive(i==(int)data.Kind);
        }
        internal void RenderRelicReward(float delta,bool live=true)
        {
            if(!rewardShowing)return;
            rewardRoot.gameObject.SetActive(live && !IsGrowthMenuOpen);
            if(!live || IsGrowthMenuOpen)return;
            rewardAge+=delta;
            rewardAlpha.alpha=Mathf.Min(Mathf.Clamp01(rewardAge/.16f),Mathf.Clamp01((4.8f-rewardAge)/.6f));
            rewardCard.localScale=Vector3.one*(1+.07f*Mathf.Sin(Mathf.Clamp01(rewardAge/.35f)*Mathf.PI));
            rewardRays.localRotation=Quaternion.Euler(0,0,rewardAge*15);
            if(rewardAge>=4.8f)NextRelicReward();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
namespace Riftrail
{
    public sealed partial class BattleView
    {
        private RectTransform offlineRoot;
        private Text offlineAmount,offlineTime,offlineHint;
        private Action claimOffline;
        public bool IsOfflineRewardOpen=>offlineRoot!=null && offlineRoot.gameObject.activeSelf;
        private void BuildOfflineReward()
        {
            offlineRoot=Rect(contentRoot,"OfflineReward");Stretch(offlineRoot);
            offlineRoot.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;
            var back=Panel(offlineRoot,"OfflineBackdrop",new Color(.03f,.10f,.09f,.92f));Stretch(back.rectTransform);back.raycastTarget=true;
            Label(offlineRoot,"다시 돌아온 걸 환영해요!",43,Gold,.5f,.81f,980,100);
            var card=Panel(offlineRoot,"OfflineCard",Cream).rectTransform;Anchor(card,.5f,.49f,980,880);
            Label(card,"영웅이 모아 둔 골드",35,Ink,.5f,.90f,900,80);
            var coin=Oval(card,"OfflineCoin",Gold,140,140);Anchor(coin.rectTransform,.5f,.74f,140,140);
            OvalAt(coin.rectTransform,"CoinCore",Hex(0xBC8A39),0,0,60,60);
            offlineAmount=Label(card,"",64,Green,.5f,.56f,900,125);
            offlineTime=Label(card,"",30,Ink,.5f,.43f,900,90);
            offlineHint=Label(card,"",26,Muted,.5f,.30f,900,110);
            var claim=MakeButton(card,"ClaimOfflineReward",.5f,.12f,850,120,Green,()=>claimOffline?.Invoke(),out _);claim.text="골드 받기";claim.fontSize=35;
            Label(offlineRoot,"1분 이상 자리를 비우면 최대 8시간까지 쌓입니다.\n저장된 성장 상태의 일반 사냥 예상 수익입니다.\n보스 진행과 유물 획득은 포함하지 않습니다.",26,Cream,.5f,.15f,980,160);
            offlineRoot.gameObject.SetActive(false);
        }
        public void ShowOfflineReward(Action claim)
        {
            if(simulation.PendingOfflineGold<=0)return;
            claimOffline=claim;CancelUpgradeHold();offlineRoot.SetAsLastSibling();offlineRoot.gameObject.SetActive(true);Render(simulation,false);
        }
        public void CloseOfflineReward() {offlineRoot.gameObject.SetActive(false);claimOffline=null;}
        private void RenderOfflineReward(BattleSimulation s)
        {
            if(!IsOfflineRewardOpen)return;
            offlineAmount.text=$"+{Number(s.PendingOfflineGold)} 골드";
            var seconds=s.PendingOfflineSeconds;offlineTime.text=$"보상 적용 시간: {seconds/3600}시간 {(seconds%3600)/60}분 {seconds%60}초";
            offlineHint.text=s.AutomaticGrowth.HasValue?$"수령 후 {StatName(s.AutomaticGrowth.Value)} 자동 강화에 사용됩니다.\n받지 않은 보상은 다시 실행해도 유지됩니다.":"골드를 받아 원하는 능력치를 강화하세요.\n받지 않은 보상은 다시 실행해도 유지됩니다.";
        }
    }
}

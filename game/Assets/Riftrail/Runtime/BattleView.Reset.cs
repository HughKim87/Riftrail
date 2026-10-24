using System;
using UnityEngine;
namespace Riftrail
{
    public sealed partial class BattleView
    {
        private Action resetAll;
        private RectTransform resetConfirmation;
        private void BuildFullReset()
        {
            Label(settingsRoot,"진행 데이터",32,Gold,.5f,.38f,900,65);
            var entry=MakeButton(settingsRoot,"RequestFullReset",.5f,.30f,800,100,Red,()=>
            {CancelUpgradeHold();resetConfirmation.SetAsLastSibling();resetConfirmation.gameObject.SetActive(true);},out _);
            entry.text="전체 초기화";entry.fontSize=32;
            Label(settingsRoot,"모든 진행을 지우고 캐릭터 선택부터 다시 시작합니다.",25,Cream,.5f,.23f,980,65);
            resetConfirmation=Rect(settingsRoot,"ResetConfirmation");Stretch(resetConfirmation);
            var shade=Panel(resetConfirmation,"ResetBackdrop",new Color(0,0,0,.9f));Stretch(shade.rectTransform);shade.raycastTarget=true;
            var card=Panel(resetConfirmation,"ResetCard",Cream).rectTransform;Anchor(card,.5f,.51f,980,1120);
            Label(card,"전체 초기화하시겠습니까?",42,Ink,.5f,.91f,900,90);
            var warning=Label(card,"삭제 후에는 복구할 수 없습니다.",32,Red,.5f,.80f,900,75);warning.fontStyle=FontStyle.Bold;
            Label(card,"삭제되는 내용\n\n스테이지 · 골드 · 성장 5종 · 자동 강화 선택\n캐릭터 선택 · 사냥/보스 진행 · 모든 기록\n유물 전체 · 환생 포인트 · 환생 능력치\n받지 않은 오프라인 보상\n\n사운드 설정도 기본값으로 돌아갑니다.\n초기화 후 캐릭터를 다시 선택합니다.",28,Ink,.5f,.49f,900,510);
            var cancel=MakeButton(card,"CancelFullReset",.255f,.12f,430,120,Green,()=>resetConfirmation.gameObject.SetActive(false),out _);cancel.text="취소";cancel.fontSize=32;
            var confirm=MakeButton(card,"ConfirmFullReset",.745f,.12f,430,120,Red,()=>
            {if(resetConfirmation.gameObject.activeSelf)resetAll?.Invoke();},out _);confirm.text="모두 삭제하고 시작";confirm.fontSize=29;
            resetConfirmation.gameObject.SetActive(false);
        }
        public void PrepareFullReset()
        {
            CancelUpgradeHold();ClearEffects(false);ClearRelicRewards();RelicRewardsReceived=0;
            CloseOfflineReward();resetConfirmation.gameObject.SetActive(false);settingsRoot.gameObject.SetActive(false);
            relicRoot.gameObject.SetActive(false);rebirthConfirm.gameObject.SetActive(false);rebirthRoot.gameObject.SetActive(false);
            purchaseAmount=1;battleId=previousLevel=-1;renderedSimulation=null;clock=strike=hit=levelFlash=0;
        }
    }
}

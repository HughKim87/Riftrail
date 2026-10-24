using UnityEngine;
using UnityEngine.UI;

namespace Riftrail
{
    public sealed partial class BattleView
    {
        private RectTransform rebirthRoot, rebirthConfirm;
        private Text rebirthEntry, rebirthSummary, rebirthWallet, rebirthStatus, rebirthAction, confirmSummary;
        private readonly Text[] permanentLabels = new Text[5], permanentCosts = new Text[5];
        private readonly Button[] permanentButtons = new Button[5];
        private Button rebirthButton;
        public bool IsRebirthOpen => rebirthRoot != null && rebirthRoot.gameObject.activeSelf;
        private void BuildRebirth()
        {
            rebirthEntry = MakeButton(contentRoot,"RebirthButton",.78f,.961f,170,120,Green,OpenRebirth,out _);
            rebirthEntry.fontSize=34;
            rebirthRoot=Rect(contentRoot,"RebirthMenu"); Stretch(rebirthRoot);
            rebirthRoot.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;
            var backdrop=Panel(rebirthRoot,"Backdrop",Hex(0x102C2C)); Stretch(backdrop.rectTransform); backdrop.raycastTarget=true;
            Label(rebirthRoot,"환생 · 영구 성장",46,Gold,.5f,.925f,850,90);
            var close=MakeButton(rebirthRoot,"CloseRebirth",.89f,.967f,150,60,Hex(0x36534B),CloseRebirth,out _); close.text="닫기"; close.fontSize=23;
            Label(rebirthRoot,"다시 시작해도 강함은 남습니다",26,Cream,.5f,.87f,980,60);
            rebirthWallet=Label(rebirthRoot,"",42,Gold,.5f,.83f,980,85);
            rebirthSummary=Label(rebirthRoot,"",25,Hex(0xBDD1B1),.5f,.773f,980,85);
            for(var i=0;i<5;i++)
            {
                var kind=GrowthOrder[i];
                var card=Panel(rebirthRoot,"PermanentCard"+i,Cream);
                Anchor(card.rectTransform,.5f,.675f-i*.10f,980,175);
                permanentLabels[i]=Label(card.rectTransform,"",28,Ink,.36f,.5f,650,145);
                permanentCosts[i]=MakeButton(card.rectTransform,"Permanent"+kind,.83f,.5f,240,110,Green,()=>
                { simulation.TryBuyPermanent(kind); Render(simulation,false); },out permanentButtons[i]);
                permanentCosts[i].fontSize=26;
            }
            Label(rebirthRoot,"초기화: 스테이지, 골드, 골드 성장, 사냥 진행\n유지: 영웅, 환생 포인트, 영구 성장, 유물",25,Cream,.5f,.187f,980,90);
            rebirthStatus=Label(rebirthRoot,"",25,Gold,.5f,.125f,980,80);
            rebirthAction=MakeButton(rebirthRoot,"RequestRebirth",.5f,.065f,980,100,Hex(0xA85A3E),()=>
            { if (!simulation.CanRebirth) return; rebirthConfirm.gameObject.SetActive(true); RenderRebirth(simulation); },out rebirthButton);
            rebirthAction.fontSize=31;
            Label(rebirthRoot,"이 화면에서는 전투가 잠시 멈춥니다",20,Muted,.5f,.015f,980,30);
            rebirthConfirm=Rect(rebirthRoot,"RebirthConfirmation"); Stretch(rebirthConfirm);
            var shade=Panel(rebirthConfirm,"ConfirmBackdrop",new Color(0,0,0,.85f)); Stretch(shade.rectTransform); shade.raycastTarget=true;
            var box=Panel(rebirthConfirm,"ConfirmCard",Cream); Anchor(box.rectTransform,.5f,.5f,980,860);
            Label(box.rectTransform,"환생하시겠습니까?",45,Ink,.5f,.88f,900,95);
            confirmSummary=Label(box.rectTransform,"",30,Ink,.5f,.53f,900,420);
            var cancel=MakeButton(box.rectTransform,"CancelRebirth",.255f,.13f,430,115,Hex(0x66776B),()=>rebirthConfirm.gameObject.SetActive(false),out _); cancel.text="계속 모험하기"; cancel.fontSize=27;
            var confirm=MakeButton(box.rectTransform,"ConfirmRebirth",.745f,.13f,430,115,Hex(0xA85A3E),()=>
            {
                if (!rebirthConfirm.gameObject.activeSelf || !simulation.TryRebirth()) return;
                ClearRelicRewards(); ClearEffects(false); rebirthConfirm.gameObject.SetActive(false); Render(simulation,false);
            },out _); confirm.text="환생"; confirm.fontSize=29;
            rebirthConfirm.gameObject.SetActive(false); rebirthRoot.gameObject.SetActive(false);
        }
        private void OpenRebirth()
        {
            if (IsChoosingHero || IsGrowthMenuOpen) return;
            CancelUpgradeHold(); rebirthRoot.SetAsLastSibling(); rebirthRoot.gameObject.SetActive(true); Render(simulation,false);
        }
        private void CloseRebirth()
        { rebirthConfirm.gameObject.SetActive(false); rebirthRoot.gameObject.SetActive(false); Render(simulation,false); }
        private void RenderRebirth(BattleSimulation s)
        {
            if (rebirthEntry==null) return;
            rebirthEntry.text=s.RebirthReward>0 ? $"환생 +{s.RebirthReward}" : "환생";
            if (!IsRebirthOpen) return;
            rebirthWallet.text=$"환생 포인트 {Number(s.RebirthPoints)} · 영구 성장";
            rebirthSummary.text=$"환생 {s.TotalRebirths}회 · 최고 보스 {s.LifetimeBestStage}\n이번 모험: 보스 {s.RunHighestClearedStage} 처치 · +{s.RebirthReward} 포인트";
            for(var i=0;i<5;i++)
            {
                var kind=GrowthOrder[i]; var level=s.PermanentLevel(kind); var max=level>=RebirthProgress.MaxLevel;
                permanentLabels[i].text=$"{StatName(kind)}  레벨 {level}\n{PermanentValue(kind,level)} {(max?"(최대)":$"-> {PermanentValue(kind,level+1)}")}\n환생 후에도 유지됩니다";
                permanentCosts[i].text=max?"최대":$"강화\n{s.PermanentUpgradeCost(kind)} 포인트";
                permanentButtons[i].interactable=s.CanBuyPermanent(kind);
            }
            rebirthButton.interactable=s.CanRebirth;
            rebirthAction.text=$"환생 · +{s.RebirthReward} 포인트";
            rebirthStatus.text=s.RebirthReward==0 ? $"보스 5를 처치하면 환생할 수 있습니다 ({s.RunHighestClearedStage}/5)\n이후 보스 5단계마다 1포인트 추가 (최대 20)" : !s.CanRebirth ? "보스와의 전투를 마친 뒤 환생해 주세요" : "더 멀리 모험하거나, 환생해 더 강해지세요";
            confirmSummary.text=$"획득: 환생 포인트 +{s.RebirthReward}\n\n스테이지 {s.Stage} → 1\n골드 {Number(s.Gold)} → 0\n골드 성장 모두 → 0레벨\n\n유지: 영웅, 환생 포인트, 영구 성장, 유물";
        }
    }
}

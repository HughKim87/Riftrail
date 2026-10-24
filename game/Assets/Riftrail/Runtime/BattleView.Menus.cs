using UnityEngine;
using UnityEngine.UI;
namespace Riftrail
{
    public sealed partial class BattleView
    {
        private RectTransform settingsRoot, relicRoot;
        private Text soundText, relicEntry, relicSummary;
        private readonly Text[] relicLabels=new Text[3];
        public bool IsSettingsOpen => settingsRoot!=null && settingsRoot.gameObject.activeSelf;
        public bool IsRelicsOpen => relicRoot!=null && relicRoot.gameObject.activeSelf;
        private RectTransform Menu(string name,string title,System.Action closeAction,out Text closeText)
        {
            var root=Rect(contentRoot,name); Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;
            var back=Panel(root,"Backdrop",Hex(0x102C2C)); Stretch(back.rectTransform);back.raycastTarget=true;
            Label(root,title,46,Gold,.5f,.90f,980,90);
            closeText=MakeButton(root,"Close"+name,.89f,.967f,150,70,Green,closeAction,out _);closeText.text="닫기";
            return root;
        }
        private void BuildMenus()
        {
            var settings=MakeButton(contentRoot,"SettingsButton",.94f,.961f,120,120,Hex(0x506B63),()=>OpenMenu(settingsRoot),out _);settings.text="설정";settings.fontSize=34;
            relicEntry=MakeButton(contentRoot,"RelicsButton",.60f,.961f,170,120,Hex(0x7A6545),()=>OpenMenu(relicRoot),out _);relicEntry.fontSize=34;
            settingsRoot=Menu("SettingsMenu","설정",()=>CloseMenu(settingsRoot),out _);
            Label(settingsRoot,"소리",36,Cream,.5f,.74f,900,80);
            soundText=MakeButton(settingsRoot,"SoundToggle",.5f,.65f,800,125,Green,()=>
            {sound.SetSoundEnabled(!sound.SoundEnabled);if(sound.SoundEnabled)sound.Click();Render(simulation,false);},out _);
            Label(settingsRoot,"클릭 · 공격 · 몬스터 피격 효과음에 함께 적용됩니다.\n설정은 다음 실행에도 유지됩니다.",28,Cream,.5f,.52f,980,120);
            Label(settingsRoot,"글꼴: 본고딕 · 오픈 폰트 라이선스 1.1\n글꼴 저작권 및 허가문은 게임에 포함되어 있습니다.",23,Muted,.5f,.12f,980,110);
            BuildFullReset();
            settingsRoot.gameObject.SetActive(false);
            relicRoot=Menu("RelicsMenu","모험의 유물",()=>CloseMenu(relicRoot),out _);
            relicSummary=Label(relicRoot,"",30,Cream,.5f,.80f,980,130);
            var tints=new[]{Hex(0xC5906D),Hex(0x90BEAB),Gold};
            for(var i=0;i<3;i++)
            {
                var kind=(RelicKind)i;
                var card=Panel(relicRoot,"RelicCard"+i,Cream);Anchor(card.rectTransform,.5f,.64f-i*.18f,980,280);
                var icon=Oval(card.rectTransform,"RelicIcon",tints[i],100,100);Anchor(icon.rectTransform,.13f,.5f,100,100);
                if(kind==RelicKind.Blade) {var blade=Box(icon.rectTransform,"Blade",Cream,0,10,16,65);blade.rectTransform.localRotation=Quaternion.Euler(0,0,-25);}
                else if(kind==RelicKind.Feather) OvalAt(icon.rectTransform,"Feather",Cream,0,0,25,65);
                else OvalAt(icon.rectTransform,"CoinCenter",Hex(0xBC8A39),0,0,40,40);
                relicLabels[i]=Label(card.rectTransform,"",31,Ink,.60f,.5f,700,220);
            }
            Label(relicRoot,"보스 처치 시 30% 확률로 유물을 얻습니다.\n같은 유물도 누적되며 환생 후에도 유지됩니다.\n종류마다 최대 1K개까지 보유할 수 있습니다.",26,Cream,.5f,.11f,980,160);
            relicRoot.gameObject.SetActive(false);
        }
        private void OpenMenu(RectTransform root)
        {
            if(IsChoosingHero || IsGrowthMenuOpen) return;
            CancelUpgradeHold();root.SetAsLastSibling();root.gameObject.SetActive(true);root.Find("Close"+root.name).SetAsLastSibling();Render(simulation,false);
        }
        private void CloseMenu(RectTransform root) {root.gameObject.SetActive(false);Render(simulation,false);}
        private void RenderMenus(BattleSimulation s)
        {
            relicEntry.text=$"유물 {Number(s.TotalRelics)}";
            soundText.text=sound.SoundEnabled?"효과음: 켜짐  ·  눌러서 끄기":"효과음: 꺼짐  ·  눌러서 켜기";
            if(!IsRelicsOpen)return;
            relicSummary.text=$"모은 유물 {Number(s.TotalRelics)}개\n영구 성장과 별도로 곱해서 적용됩니다.";
            for(var i=0;i<3;i++) {var kind=(RelicKind)i;var count=s.RelicCount(kind);
                relicLabels[i].text=$"{RelicCatalog.Name(kind)}  ·  {Number(count)}개\n{RelicCatalog.Effect(kind,count)}\n{(count==0?"보스에게서 발견해 보세요":"모험을 거듭할수록 더 강해집니다")}";}
        }
    }
}

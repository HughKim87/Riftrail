#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Riftrail
{
    /// <summary>Two real player processes exercise quit/save and startup/load using a task-owned preference key.</summary>
    public sealed class GamePersistenceProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"-riftrail-persistence-test");
            var mode=args[index+1];var key=args[index+2];var expectedPath=args[index+3];
            var controller=GetComponent<AutoBattleController>();var battle=controller.Battle;var view=GetComponent<BattleView>();
            GetComponent<GameAudio>().SetSoundEnabled(false);
            battle.Attacked-=view.ShowAttack;battle.GoldEarned-=view.ShowGold;battle.RelicFound-=view.ShowRelicReward;
            if(mode=="reset")
            {
                var passed=controller.HasChosenHero && GameSaveStore.HasSave(key);
                RebirthSaveStore.Save(battle.ExportPermanentProgress(),key+".Legacy");
                var buttons=GetComponentsInChildren<Button>(true);
                foreach(var name in new[]{"SettingsButton","RequestFullReset","ConfirmFullReset"})Array.Find(buttons,button=>button.name==name).onClick.Invoke();
                passed &= !controller.HasChosenHero && view.IsChoosingHero && battle.TotalRelics==0 && battle.RebirthPoints==0 && battle.Gold==0
                    && !GameSaveStore.HasSave(key) && !RebirthSaveStore.HasSave(key+".Legacy");
                Debug.Log($"RIFTRAIL persisted full reset passed {passed}: current/legacy keys cleared and fresh selection.");
                Application.Quit(passed?0:1);yield break;
            }
            if(mode=="fresh")
            {
                var passed=!controller.HasChosenHero && view.IsChoosingHero && battle.Stage==1 && battle.Gold==0 && battle.TotalRelics==0 && battle.LifetimeBestStage==0
                    && !GameSaveStore.HasSave(key) && !RebirthSaveStore.HasSave(key+".Legacy");
                controller.EndIsolatedSaveTest();
                Debug.Log($"RIFTRAIL reset survives process restart passed {passed}.");Application.Quit(passed?0:1);yield break;
            }
            if(mode=="offline-pending")
            {
                battle.IsPaused=true;
                var passed=view.IsOfflineRewardOpen && battle.PendingOfflineGold>0;
                Debug.Log($"RIFTRAIL unclaimed offline reopen checkpoint passed {passed}.");
                Application.Quit(passed?0:1);yield break;
            }
            if(mode=="offline-read")
            {
                battle.IsPaused=true;
                var passed=controller.HasChosenHero && view.IsOfflineRewardOpen && battle.PendingOfflineGold>0 && battle.PendingOfflineSeconds>=3600;
                var amount=battle.PendingOfflineGold;var total=battle.TotalGoldEarned;
                yield return new WaitForSecondsRealtime(.2f);
                Array.Find(GetComponentsInChildren<Button>(true),button=>button.name=="ClaimOfflineReward").onClick.Invoke();
                passed &= !view.IsOfflineRewardOpen && battle.PendingOfflineGold==0 && battle.TotalGoldEarned==total+amount && !battle.TryClaimOffline();
                File.WriteAllText(expectedPath,GameSaveStore.Encode(battle.ExportGame()));
                Debug.Log($"RIFTRAIL offline cross-process claim passed {passed}: pending modal, exact reward and duplicate claim guard.");
                Application.Quit(passed?0:1);yield break;
            }
            if(mode=="read")
            {
                battle.IsPaused=true;
                var actual=battle.ExportGame();GameSaveStore.TryDecode(File.ReadAllText(expectedPath),out var expected);
                if(expected!=null)expected.savedUtcSeconds=actual.savedUtcSeconds;
                var passed=expected!=null && controller.HasChosenHero && !view.IsChoosingHero && !view.IsOfflineRewardOpen && GameSaveStore.TryLoad(out var saved,key)
                    && GameSaveStore.Encode(expected)==GameSaveStore.Encode(actual)
                    && battle.Stage>=6 && battle.Gold>0 && battle.Hero==HeroKind.Kunoichi && battle.AutomaticGrowth==RebirthUpgrade.CritDamage;
                var level=battle.GrowthLevel(RebirthUpgrade.CritDamage);battle.IsPaused=false;
                for(var i=0;i<20000 && battle.GrowthLevel(RebirthUpgrade.CritDamage)==level;i++)battle.Advance(.05);
                passed &= battle.GrowthLevel(RebirthUpgrade.CritDamage)>level;
                controller.EndIsolatedSaveTest();PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Debug.Log($"RIFTRAIL cross-process resume passed {passed}: hero, stage, exact gold, all levels, automatic selection and continued spending.");
                Application.Quit(passed?0:1);yield break;
            }
            if((mode!="write" && mode!="offline-write" && mode!="migration-write") || controller.HasChosenHero) {controller.EndIsolatedSaveTest();Application.Quit(1);yield break;}
            Array.Find(GetComponentsInChildren<Button>(),button=>button.name=="SelectHero2").onClick.Invoke();
            for(var i=0;i<400000 && battle.Stage<6;i++)
            {
                while(battle.CanUpgrade)battle.TryUpgrade();
                if(battle.CanChallengeBoss && battle.HasBossPower)battle.TryChallengeBoss();battle.Advance(.05);
            }
            for(var i=0;i<100000 && battle.Gold<2000;i++)battle.Advance(.05);
            battle.ToggleAutomaticGrowth(RebirthUpgrade.CritDamage);
            for(var i=0;i<10000 && battle.Gold==0;i++)battle.Advance(.05);
            battle.IsPaused=true;
            if(battle.Stage<6 || battle.Gold==0 || battle.AutomaticGrowth!=RebirthUpgrade.CritDamage) {Application.Quit(1);yield break;}
            var snapshot=battle.ExportGame();
            if(mode=="offline-write")
            {
                snapshot.savedUtcSeconds=DateTimeOffset.UtcNow.ToUnixTimeSeconds()-3600;
                controller.EndIsolatedSaveTest();GameSaveStore.Save(snapshot,key);
            }
            if(mode=="migration-write")
            {
                snapshot.version=1;snapshot.levels=new[]{10,1,2,3,10};snapshot.automatic=4;
                controller.EndIsolatedSaveTest();PlayerPrefs.SetString(key,JsonUtility.ToJson(snapshot));PlayerPrefs.Save();
                File.WriteAllText(expectedPath,JsonUtility.ToJson(snapshot));
            }
            else File.WriteAllText(expectedPath,GameSaveStore.Encode(snapshot));
            Debug.Log("RIFTRAIL persistence writer requests quit; controller lifecycle must flush the exact checkpoint.");
            Application.Quit(0);
        }
    }
}
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Riftrail
{
    /// <summary>Opt-in development-player smoke check; inactive in normal play.</summary>
    public sealed class BattleSmokeProbe : MonoBehaviour
    {
        private bool failed;
        private bool active;
        private int attacks;
        private BattleSimulation battle;

        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-riftrail-smoke") < 0) yield break;
            active = true;
            Application.logMessageReceived += OnLog;
            battle = GetComponent<AutoBattleController>().Battle;
            battle.Attacked += OnAttack;
            var view = GetComponent<BattleView>();
            var gameAudio=GetComponent<GameAudio>();
            failed |= !gameAudio.ClipsReady;
            var screenshotIndex = Array.IndexOf(args, "-riftrail-screenshot");
            var capture = screenshotIndex >= 0 && screenshotIndex + 1 < args.Length ? args[screenshotIndex + 1] : null;
            yield return new WaitForSecondsRealtime(.3f);
            GetComponent<AutoBattleController>().TogglePause();
            failed |= !battle.IsPaused || battle.Gold != 0 || attacks != 0 || !view.IsChoosingHero;
            failed |= !ControlsFit() || !AllKorean();
            if (capture != null)
            {
                CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "hero-selection.png"));
                var gallery = view.CreateArtGallery();
                CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "character-gallery.png"), 1080, 1920);
                Destroy(gallery);
                yield return null;
            }
            var buttons = GetComponentsInChildren<Button>();
            var choose = Array.Find(buttons, button => button.name == "SelectHero1");
            choose.onClick.Invoke();
            failed |= !GetComponent<AutoBattleController>().HasChosenHero || view.SelectedHero != 1 || battle.Hero != HeroKind.Wuxia || battle.IsPaused || view.IsChoosingHero;
            failed |= Array.Exists(buttons,button=>button.name=="HeroButton" || button.name=="PauseButton");
            var rebirth=Array.Find(buttons,button=>button.name=="RebirthButton");
            failed |= rebirth==null || ((Image)rebirth.targetGraphic).color.a<.9f;
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"hero-game.png"));
            Action pause=GetComponent<AutoBattleController>().TogglePause;
            var upgrade = Array.Find(buttons, button => button.name == "UpgradeButton");
            var boss = Array.Find(buttons, button => button.name == "BossButton");
            if (upgrade == null || boss == null)
            {
                Debug.LogError("Progression controls are missing.");
                Application.Quit(1); yield break;
            }
            pause();
            var pausedHealth = battle.Enemy.Health;
            var pausedGold = battle.Gold;
            yield return new WaitForSecondsRealtime(.4f);
            failed |= !battle.IsPaused || pausedHealth != battle.Enemy.Health || pausedGold != battle.Gold;
            pause();
            failed |= battle.IsPaused;
            var deadline = Time.realtimeSinceStartup + 90;
            var held = upgrade as HoldUpgradeButton;
            if (held == null)
            {
                Debug.LogError("Hold-to-upgrade control is missing.");
                Application.Quit(1); yield break;
            }
            // A disabled button must not start a hold or spend currency.
            var mouse = Pointer(upgrade, -1);
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerDownHandler);
            failed |= held.IsHolding || battle.UpgradeLevel != 0;
            var seen = new HashSet<MonsterKind>();
            while (battle.Gold < 150 && Time.realtimeSinceStartup < deadline)
            {
                if (battle.Encounter==EncounterKind.Farming && seen.Add(battle.Enemy.Kind) && capture != null)
                    CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "monster-" + battle.Enemy.Kind + ".png"));
                yield return null;
            }
            failed |= seen.Count != 1 || !seen.Contains(MonsterKind.Slime);
            failed |= battle.Gold < 150;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(mouse, hits);
            failed |= hits.Count == 0 || hits[0].gameObject != upgrade.gameObject;
            var goldBeforeTap = battle.Gold;
            var tapCost = battle.UpgradeCost;
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerDownHandler);
            failed |= battle.UpgradeLevel != 1 || battle.Gold != goldBeforeTap - tapCost;
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerClickHandler);
            failed |= battle.UpgradeLevel != 1 || held.IsHolding; // no purchase on release/click

            var beforeHold = battle.UpgradeLevel;
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.9f);
            failed |= battle.UpgradeLevel < beforeHold + 2 || battle.Gold < 0 || !held.IsHolding;
            pause();
            var pausedLevel = battle.UpgradeLevel;
            failed |= held.IsHolding;
            yield return new WaitForSecondsRealtime(.25f);
            failed |= battle.UpgradeLevel != pausedLevel;
            pause();
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(upgrade.gameObject, mouse, ExecuteEvents.pointerClickHandler);
            failed |= battle.UpgradeLevel != pausedLevel;

            while (!battle.CanUpgrade && Time.realtimeSinceStartup < deadline) yield return null;
            var touch = Pointer(upgrade, 7);
            ExecuteEvents.Execute(upgrade.gameObject, touch, ExecuteEvents.pointerDownHandler);
            var touchLevel = battle.UpgradeLevel;
            failed |= !held.IsHolding || touchLevel != pausedLevel + 1;
            var otherTouch = Pointer(upgrade, 8);
            ExecuteEvents.Execute(upgrade.gameObject, otherTouch, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(upgrade.gameObject, otherTouch, ExecuteEvents.pointerUpHandler);
            failed |= !held.IsHolding || battle.UpgradeLevel != touchLevel;
            ExecuteEvents.Execute(upgrade.gameObject, touch, ExecuteEvents.pointerExitHandler);
            failed |= held.IsHolding;
            yield return new WaitForSecondsRealtime(.45f);
            ExecuteEvents.Execute(upgrade.gameObject, touch, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(upgrade.gameObject, touch, ExecuteEvents.pointerClickHandler);
            failed |= battle.UpgradeLevel != touchLevel;
            while(battle.CanUpgrade && !battle.HasBossPower)battle.TryUpgrade();
            GetComponent<BattleView>().Render(battle,false);
            var testedLevel = battle.UpgradeLevel;
            while (!battle.CanChallengeBoss && Time.realtimeSinceStartup < deadline) yield return null;
            failed |= !boss.interactable;
            Debug.Log($"RIFTRAIL hold input checks: tap, repeat, pause cancellation, pointer exit, second touch; level {testedLevel}, gold {battle.Gold}, passed {!failed}.");
            if (capture != null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "traits-farming.png"));
            while(battle.Phase!=BattlePhase.Victory && Time.realtimeSinceStartup<deadline) yield return null;
            failed |= battle.Phase!=BattlePhase.Victory || !boss.IsInteractable();
            Debug.Log($"RIFTRAIL boss button during farming death transition: interactable {boss.IsInteractable()}, passed {!failed}.");
            boss.onClick.Invoke();
            failed |= battle.Encounter != EncounterKind.Boss || battle.Enemy.Health != battle.Enemy.MaxHealth;
            yield return new WaitForSecondsRealtime(.3f);
            failed |= !boss.IsInteractable() || !battle.CanAbandonBoss;
            var abandonGold=battle.Gold;var abandonRelics=battle.TotalRelics;var abandonPoints=battle.RebirthPoints;
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"boss-abandon.png"));
            boss.onClick.Invoke();
            failed |= battle.Encounter!=EncounterKind.Farming || battle.Gold!=abandonGold || battle.TotalRelics!=abandonRelics || battle.RebirthPoints!=abandonPoints || !battle.AutoBossAttempted;
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"boss-abandoned.png"));
            boss.onClick.Invoke();failed |= !battle.CanAbandonBoss;
            Debug.Log($"RIFTRAIL boss abandon UI passed {!failed}: active abandon button, preserved progress, normal farming and manual retry.");
            yield return new WaitForSecondsRealtime(1.5f);
            if (capture != null) CaptureOffscreen(capture);
            failed |= !ControlsFit();
            Screen.SetResolution(540, 1170, false);
            yield return new WaitForSecondsRealtime(.5f);
            failed |= !ControlsFit();
            if (capture != null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "traits-tall.png"), 540, 1170);
            Screen.SetResolution(768, 1024, false);
            yield return new WaitForSecondsRealtime(.5f);
            failed |= !ControlsFit();
            if (capture != null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture), "traits-tablet.png"), 768, 1024);
            while (!(battle.Encounter==EncounterKind.Boss && battle.Phase==BattlePhase.Victory) && battle.Stage==1 && Time.realtimeSinceStartup<deadline) yield return null;
            if(battle.Stage==1)
            {
                yield return new WaitForSecondsRealtime(.9f);
                var heroRect=Array.Find(GetComponentsInChildren<RectTransform>(),rect=>rect.name=="SwordMaster");
                failed |= heroRect==null || heroRect.anchoredPosition.x<=0;
                if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"boss-exit.png"));
            }
            while (battle.Stage == 1 && Time.realtimeSinceStartup < deadline) yield return null;
            var passed = !failed && attacks > 0
                && battle.Stage == 2 && battle.BossesCleared == 1 && battle.UpgradeLevel == testedLevel;
            if (passed && capture != null) CaptureTraitPreviews(Path.GetDirectoryName(capture));
            if(passed) yield return CheckSettingsAndRelics(capture);
            if (passed) yield return CheckGrowthUI(capture);
            if(passed && !failed)yield return CheckAutomaticUI(capture);
            if (passed && !failed) yield return CheckRebirthUI(capture);
            if(passed && !failed && capture!=null)CaptureRelicRewards(Path.GetDirectoryName(capture));
            if(passed && !failed)yield return CheckOfflineUI(capture);
            if(passed && !failed)yield return CheckFullResetUI(capture);
            passed &= !failed;
            Debug.Log(passed ? "RIFTRAIL battle runtime smoke passed." : "RIFTRAIL battle runtime smoke FAILED.");
            Application.Quit(passed ? 0 : 1);
        }

        private IEnumerator CheckOfflineUI(string capture)
        {
            var view=GetComponent<BattleView>();view.ClearRelicRewards();
            battle.ToggleAutomaticGrowth(RebirthUpgrade.Attack);battle.MarkSavedAt(1000);
            failed |= !battle.AccrueOffline(4600) || battle.PendingOfflineGold<=0;
            view.ShowOfflineReward(GetComponent<AutoBattleController>().ClaimOfflineReward);
            var elapsed=battle.EnemyElapsed;var gold=battle.Gold;
            yield return new WaitForSecondsRealtime(.25f);
            failed |= !view.IsOfflineRewardOpen || battle.EnemyElapsed!=elapsed || battle.Gold!=gold || !ControlsFit() || !AllKorean();
            var claim=Array.Find(GetComponentsInChildren<Button>(true),button=>button.name=="ClaimOfflineReward");
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(Pointer(claim,-1),hits);
            failed |= hits.Count==0 || hits[0].gameObject!=claim.gameObject;
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"offline-reward.png"));
            var amount=battle.PendingOfflineGold;var total=battle.TotalGoldEarned;var level=battle.UpgradeLevel;
            claim.onClick.Invoke();claim.onClick.Invoke();
            failed |= view.IsOfflineRewardOpen || battle.PendingOfflineGold!=0 || battle.TotalGoldEarned!=total+amount || battle.UpgradeLevel<=level || battle.TryClaimOffline();
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"offline-claimed.png"));
            Debug.Log($"RIFTRAIL offline reward UI passed {!failed}: displayed time/gold, modal raycast, one-time claim and automatic growth.");
        }
        private IEnumerator CheckFullResetUI(string capture)
        {
            var view=GetComponent<BattleView>();var buttons=GetComponentsInChildren<Button>(true);
            Action<string> click=name=>Array.Find(buttons,button=>button.name==name).onClick.Invoke();
            click("SettingsButton");click("RequestFullReset");yield return new WaitForSecondsRealtime(.1f);
            var before=GameSaveStore.Encode(battle.ExportGame());
            failed |= !ControlsFit() || !AllKorean();
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"reset-confirmation.png"));
            click("CancelFullReset");failed |= GameSaveStore.Encode(battle.ExportGame())!=before;
            click("RequestFullReset");click("ConfirmFullReset");click("ConfirmFullReset");yield return null;
            failed |= GetComponent<AutoBattleController>().HasChosenHero || !view.IsChoosingHero || !battle.IsPaused || battle.Stage!=1 || battle.Gold!=0
                || battle.TotalRelics!=0 || battle.RebirthPoints!=0 || battle.TotalRebirths!=0 || battle.PendingOfflineGold!=0 || battle.AutomaticGrowth.HasValue;
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade)))failed |= battle.GrowthLevel(kind)!=0 || battle.PermanentLevel(kind)!=0;
            failed |= !GetComponent<GameAudio>().SoundEnabled || !ControlsFit();
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"reset-character-selection.png"));
            Array.Find(GetComponentsInChildren<Button>(),button=>button.name=="SelectHero2").onClick.Invoke();
            failed |= !GetComponent<AutoBattleController>().HasChosenHero || battle.Hero!=HeroKind.Kunoichi || battle.IsPaused;
            Debug.Log($"RIFTRAIL full reset UI passed {!failed}: warning, cancellation, confirmed wipe, duplicate guard, fresh hero selection and restart.");
        }
        private bool AllKorean()
        {
            foreach(var label in GetComponentsInChildren<Text>(true))
                if(System.Text.RegularExpressions.Regex.IsMatch(System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(label.text,@"<[^>]*>",""),@"(?<=\d)[KMGTPE]",""),"[A-Za-z]")) {Debug.LogError("Untranslated UI: "+label.text);return false;}
            return true;
        }
        private IEnumerator CheckSettingsAndRelics(string capture)
        {
            var view=GetComponent<BattleView>(); var audio=GetComponent<GameAudio>();
            var buttons=GetComponentsInChildren<Button>(true);
            Action<string> click=name=>Array.Find(buttons,b=>b.name==name).onClick.Invoke();
            failed |= audio.PlayedAttacks==0 || audio.PlayedHits==0 || !AllKorean();
            click("SettingsButton");yield return null;
            var elapsed=battle.EnemyElapsed;var clicks=audio.PlayedClicks;
            click("SoundToggle"); audio.Click();
            var mutedClicks=audio.PlayedClicks;var mutedAttacks=audio.PlayedAttacks;var mutedHits=audio.PlayedHits;
            audio.OnAttack(new BattleAttack(1,false,HeroKind.SwordMaster,false,false));
            audio.Click();yield return new WaitForSecondsRealtime(.3f);
            failed |= !view.IsSettingsOpen || audio.SoundEnabled || audio.PlayedClicks!=mutedClicks || audio.PlayedAttacks!=mutedAttacks || audio.PlayedHits!=mutedHits || battle.EnemyElapsed!=elapsed;
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"settings-ko.png"));
            click("SoundToggle"); failed |= !audio.SoundEnabled || audio.PlayedClicks<=clicks;
            click("CloseSettingsMenu");click("RelicsButton");yield return new WaitForSecondsRealtime(.1f);
            failed |= !view.IsRelicsOpen || !ControlsFit() || !AllKorean();
            var closeRelics=Array.Find(buttons,b=>b.name=="CloseRelicsMenu");
            var closeHits=new List<RaycastResult>();EventSystem.current.RaycastAll(Pointer(closeRelics,-1),closeHits);
            failed |= closeHits.Count==0 || closeHits[0].gameObject!=closeRelics.gameObject;
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"relics-ko.png"));
            click("CloseRelicsMenu");
            var key="Riftrail.Sound.Test."+Guid.NewGuid().ToString("N");
            GameObject first=null,second=null;
            try
            {
                first=new GameObject("SoundPreferenceTest");var initial=first.AddComponent<GameAudio>();initial.Initialize(true,key);initial.SetSoundEnabled(false);
                second=new GameObject("SoundPreferenceReload");var restored=second.AddComponent<GameAudio>();restored.Initialize(true,key);
                failed |= restored.SoundEnabled;
            }
            finally {if(first!=null)Destroy(first);if(second!=null)Destroy(second);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            Debug.Log($"RIFTRAIL Korean UI/settings/relic menu/audio mute and isolated preference reload checks passed {!failed}.");
        }

        private IEnumerator CheckGrowthUI(string capture)
        {
            var view=GetComponent<BattleView>();
            var audio=GetComponent<GameAudio>();audio.SetSoundEnabled(false);
            battle.Attacked-=view.ShowAttack; battle.GoldEarned-=view.ShowGold;
            try { for(var i=0;i<60000 && battle.Gold<15000;i++) battle.Advance(.05); }
            finally { battle.Attacked+=view.ShowAttack; battle.GoldEarned+=view.ShowGold;audio.SetSoundEnabled(true); }
            view.Render(battle,false); yield return null;
            var buttons=GetComponentsInChildren<Button>(true);
            Action<string> click=name=>Array.Find(buttons,b=>b.name==name).onClick.Invoke();
            failed |= Array.Exists(buttons,b=>b.name=="GrowthButton" || b.name=="CloseGrowth") || view.IsGrowthMenuOpen;
            var speed=Array.Find(buttons,b=>b.name=="GrowthBuyAttackSpeed");
            var pointer=Pointer(speed,-1);
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            failed |= hits.Count==0 || hits[0].gameObject!=speed.gameObject || !speed.IsInteractable();
            Debug.Log($"RIFTRAIL growth raycast/entry passed {!failed}; top {(hits.Count>0?hits[0].gameObject.name:"none")}");
            var before=battle.Gold; var cost=battle.GrowthCost(RebirthUpgrade.AttackSpeed);
            ExecuteEvents.Execute(speed.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(speed.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(speed.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            failed |= battle.GrowthLevel(RebirthUpgrade.AttackSpeed)!=1 || battle.Gold!=before-cost;
            Debug.Log($"RIFTRAIL growth single passed {!failed}, level {battle.GrowthLevel(RebirthUpgrade.AttackSpeed)}");
            var attackCount=attacks;
            ExecuteEvents.Execute(speed.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.75f);
            failed |= battle.GrowthLevel(RebirthUpgrade.AttackSpeed)<4;
            // Changing the batch size releases the held pointer instead of buying an unexpected batch.
            click("Quantity10");
            failed |= ((HoldUpgradeButton)speed).IsHolding;
            yield return new WaitForSecondsRealtime(1.8f);
            failed |= attacks<=attackCount;
            Debug.Log($"RIFTRAIL growth hold/live passed {!failed}, attacks {attackCount}->{attacks}");
            var level=battle.GrowthLevel(RebirthUpgrade.AttackSpeed);
            before=battle.Gold; var count=battle.GrowthPurchaseCount(RebirthUpgrade.AttackSpeed,10,out cost);
            speed.onClick.Invoke();
            failed |= count!=10 || battle.GrowthLevel(RebirthUpgrade.AttackSpeed)!=level+10 || battle.Gold!=before-cost;
            Debug.Log($"RIFTRAIL growth x10 passed {!failed}, count {count}, before {before}, cost {cost}, after {battle.Gold}, level {level}->{battle.GrowthLevel(RebirthUpgrade.AttackSpeed)}");
            click("QuantityMax");
            var crit=Array.Find(buttons,b=>b.name=="GrowthBuyCritChance");
            count=battle.GrowthPurchaseCount(RebirthUpgrade.CritChance,0,out cost); before=battle.Gold;
            crit.onClick.Invoke();
            failed |= count<=0 || battle.GrowthLevel(RebirthUpgrade.CritChance)!=count || battle.Gold!=before-cost;
            Debug.Log($"RIFTRAIL growth MAX passed {!failed}, count {count}, cost {cost}, level {battle.GrowthLevel(RebirthUpgrade.CritChance)}");
            click("Quantity1");
            // Rebirth owns input and cancels all growth holds while it freezes combat.
            ExecuteEvents.Execute(speed.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            click("RebirthButton"); yield return null;
            failed |= ((HoldUpgradeButton)speed).IsHolding || !view.IsGrowthMenuOpen || speed.IsInteractable();
            var frozen=attacks; before=battle.Gold;
            var overlayHits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,overlayHits);
            failed |= overlayHits.Count==0 || overlayHits[0].gameObject==speed.gameObject;
            speed.onClick.Invoke(); yield return new WaitForSecondsRealtime(.25f);
            failed |= attacks!=frozen || battle.Gold!=before;
            Debug.Log($"RIFTRAIL growth modal passed {!failed}, top {(overlayHits.Count>0?overlayHits[0].gameObject.name:"none")}");
            click("CloseRebirth");
            failed |= !ControlsFit();
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"growth-tablet.png"),768,1024);
            Screen.SetResolution(540,960,false); yield return new WaitForSecondsRealtime(.3f);
            failed |= !ControlsFit();
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"growth-phone.png"));
            Debug.Log($"RIFTRAIL persistent growth: five rows, raycast, hold, live combat, x10/MAX exact spending, modal cancellation, portrait/tablet; passed {!failed}.");
        }

        private IEnumerator CheckAutomaticUI(string capture)
        {
            var view=GetComponent<BattleView>();var buttons=GetComponentsInChildren<Button>(true);
            Action<string> click=name=>Array.Find(buttons,b=>b.name==name).onClick.Invoke();
            click("AutoGrowthAttackSpeed");failed |= battle.AutomaticGrowth!=RebirthUpgrade.AttackSpeed;
            click("AutoGrowthGold");failed |= battle.AutomaticGrowth!=RebirthUpgrade.Gold;
            click("AutoGrowthGold");failed |= battle.AutomaticGrowth.HasValue;
            click("AutoGrowthAttack");var before=battle.UpgradeLevel;
            var deadline=Time.realtimeSinceStartup+15;
            while(battle.UpgradeLevel==before && Time.realtimeSinceStartup<deadline)yield return null;
            failed |= battle.UpgradeLevel<=before || battle.CanUpgrade || !AllKorean() || !ControlsFit();
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"automatic-growth.png"));
            click("SettingsButton");yield return null;
            var frozen=battle.UpgradeLevel;var gold=battle.Gold;yield return new WaitForSecondsRealtime(.3f);
            failed |= battle.UpgradeLevel!=frozen || battle.Gold!=gold;
            click("CloseSettingsMenu");click("AutoGrowthAttack");failed |= battle.AutomaticGrowth.HasValue;
            Debug.Log($"RIFTRAIL automatic growth UI checks passed {!failed}: exclusive selection, toggle off, live gold spending and modal freeze.");
        }
        private void CaptureRelicRewards(string directory)
        {
            var view=GetComponent<BattleView>();
            failed |= view.RelicRewardsReceived!=battle.TotalRelics;
            var preview=new BattleSimulation(new RebirthProgress{relicSeed=12345});
            var kinds=new RelicKind[2];var counts=new int[2];var before=new double[2];var after=new double[2];
            preview.RelicFound+=kind=>
            {
                var index=preview.RelicCount(kind)==1?0:1;
                if(counts[index]>0)return;
                kinds[index]=kind;counts[index]=preview.RelicCount(kind);before[index]=preview.LastRelicBefore;after[index]=preview.LastRelicAfter;
            };
            for(var i=0;i<400000 && (counts[0]==0 || counts[1]==0);i++)AutoStep(preview);
            failed |= counts[0]!=1 || counts[1]<2;
            view.ClearRelicRewards();
            for(var i=0;i<2;i++)view.ShowRelicReward(kinds[i],counts[i],before[i],after[i]);
            view.Render(battle,false);view.RenderRelicReward(.4f);
            CaptureOffscreen(Path.Combine(directory,"relic-first-found.png"));
            view.RenderRelicReward(5f);view.RenderRelicReward(.4f);
            var labels=GetComponentsInChildren<Text>();
            failed |= !Array.Exists(labels,label=>label.text==RelicCatalog.Name(kinds[1])) || !Array.Exists(labels,label=>label.text.Contains("보유 1개 → 2개")) || !AllKorean();
            CaptureOffscreen(Path.Combine(directory,"relic-duplicate.png"));
            CaptureOffscreen(Path.Combine(directory,"relic-duplicate-tablet.png"),768,1024);
            view.ClearRelicRewards();
            Debug.Log($"RIFTRAIL earned relic feedback checks passed {!failed}: first find, duplicate counts, bonus/stat deltas and ordered notification queue.");
        }

        private IEnumerator CheckRebirthUI(string capture)
        {
            var view=GetComponent<BattleView>();
            // Accelerated earned progression prepares the UI scenario without injecting currency or points.
            var audio=GetComponent<GameAudio>();audio.SetSoundEnabled(false);
            battle.Attacked-=view.ShowAttack; battle.GoldEarned-=view.ShowGold;
            try
            {
                for(var i=0;i<200000 && !(battle.RunHighestClearedStage>=25 && battle.Encounter==EncounterKind.Farming);i++)
                {
                    while(battle.CanUpgrade) battle.TryUpgrade();
                    if(battle.CanChallengeBoss && battle.HasBossPower) battle.TryChallengeBoss();
                    battle.Advance(.05);
                }
            }
            finally { battle.Attacked+=view.ShowAttack; battle.GoldEarned+=view.ShowGold;audio.SetSoundEnabled(true); }
            view.Render(battle,false);
            var buttons=GetComponentsInChildren<Button>(true);
            Action<string> click=name=>Array.Find(buttons,b=>b.name==name).onClick.Invoke();
            click("RebirthButton");
            var health=battle.Enemy.Health; var elapsed=battle.EnemyElapsed;
            var stage=battle.Stage; var gold=battle.Gold; var level=battle.UpgradeLevel; var hero=battle.Hero;
            yield return new WaitForSecondsRealtime(.35f);
            failed |= !view.IsRebirthOpen || battle.RebirthReward!=5 || battle.Enemy.Health!=health || battle.EnemyElapsed!=elapsed;
            failed |= !ControlsFit();
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"rebirth-menu.png"),768,1024);
            click("RequestRebirth");
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"rebirth-confirm.png"),768,1024);
            click("CancelRebirth");
            failed |= battle.Stage!=stage || battle.Gold!=gold || battle.UpgradeLevel!=level || battle.TotalRebirths!=0;
            click("RequestRebirth"); click("ConfirmRebirth"); click("ConfirmRebirth");
            failed |= battle.Stage!=1 || battle.Gold!=0 || battle.UpgradeLevel!=0 || battle.RebirthPoints!=5
                || battle.TotalRebirths!=1 || battle.Hero!=hero
                || battle.GrowthLevel(RebirthUpgrade.AttackSpeed)!=0 || battle.GrowthLevel(RebirthUpgrade.CritChance)!=0
                || battle.GrowthLevel(RebirthUpgrade.CritDamage)!=0 || battle.GrowthLevel(RebirthUpgrade.Gold)!=0;
            foreach(RebirthUpgrade kind in Enum.GetValues(typeof(RebirthUpgrade))) click("Permanent"+kind);
            click("PermanentGold");
            failed |= battle.RebirthPoints!=0 || battle.PermanentAttackLevel!=1 || battle.PermanentGoldLevel!=1 || battle.AttackPower!=(long)Math.Round(11*(1+.03*battle.RelicCount(RelicKind.Blade)))
                || battle.PermanentLevel(RebirthUpgrade.AttackSpeed)!=1 || battle.PermanentLevel(RebirthUpgrade.CritChance)!=1
                || battle.PermanentLevel(RebirthUpgrade.CritDamage)!=1;
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"rebirth-upgrades.png"),768,1024);
            Screen.SetResolution(540,960,false); yield return new WaitForSecondsRealtime(.3f);
            failed |= !ControlsFit();
            if(capture!=null) CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"rebirth-phone.png"));
            click("CloseRebirth");
            click("RelicsButton");yield return new WaitForSecondsRealtime(.1f);
            failed |= !AllKorean();
            if(capture!=null)CaptureOffscreen(Path.Combine(Path.GetDirectoryName(capture),"relics-collected-ko.png"));
            click("CloseRelicsMenu");
            yield return new WaitForSecondsRealtime(.4f);
            failed |= view.IsRebirthOpen || battle.EnemyElapsed<=0;
            Debug.Log($"RIFTRAIL rebirth UI checks: earned 25 bosses, menu freeze, cancel, confirm, duplicate guard, upgrades, portrait/tablet, resume; passed {!failed}.");
        }

        private void CaptureTraitPreviews(string directory)
        {
            var preview=new BattleSimulation(); preview.TrySelectHero(HeroKind.Wuxia);
            var view=GetComponent<BattleView>();
            for(var stage=1;stage<=10;stage++)
            {
                for(var i=0;i<200000 && preview.Stage<stage;i++) AutoStep(preview);
                if(preview.Stage!=stage) { failed=true; return; }
                view.Render(preview,false);
                CaptureOffscreen(Path.Combine(directory,$"stage-{stage}-normal.png"));
                for(var i=0;i<200000 && !preview.CanChallengeBoss;i++) AutoStep(preview,false);
                while(preview.CanUpgrade) preview.TryUpgrade();
                if(!preview.TryChallengeBoss()) { failed=true; return; }
                view.Render(preview,false);
                CaptureOffscreen(Path.Combine(directory,$"stage-{stage}-boss.png"));
                for(var i=0;i<200000 && preview.Stage==stage;i++) AutoStep(preview);
            }
            view.Render(battle,false);
            Debug.Log("RIFTRAIL ten stage-family normal/boss UI fixtures captured from earned model progression.");
        }
        private static void AutoStep(BattleSimulation b,bool challenge=true)
        {
            if(b.CanUpgrade) b.TryUpgrade();
            if(challenge && b.CanChallengeBoss && b.HasBossPower) b.TryChallengeBoss();
            b.Advance(.05);
        }

        private static PointerEventData Pointer(Button button,int id)
        {
            return new PointerEventData(EventSystem.current) { pointerId=id, button=PointerEventData.InputButton.Left,
                position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position) };
        }
        private bool ControlsFit()
        {
            Canvas.ForceUpdateCanvases();
            var bounds=Screen.safeArea; var corners=new Vector3[4];
            foreach(var button in GetComponentsInChildren<Button>())
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var pixel=RectTransformUtility.WorldToScreenPoint(null,corner);
                    if(pixel.x<bounds.xMin-1 || pixel.x>bounds.xMax+1 || pixel.y<bounds.yMin-1 || pixel.y>bounds.yMax+1) return false;
                }
            }
            return true;
        }
        private void CaptureOffscreen(string path,int width=540,int height=960)
        {
            var canvas=GetComponentInChildren<Canvas>(); var camera=Camera.main;
            var oldMode=canvas.renderMode; var oldCamera=canvas.worldCamera; var oldDistance=canvas.planeDistance;
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active; var oldAspect=camera.aspect;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            Texture2D texture=null;
            try
            {
                camera.targetTexture=target; camera.aspect=(float)width/height;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10;
                Canvas.ForceUpdateCanvases(); GetComponent<BattleView>().SendMessage("FitContent"); Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active=target;
                texture=new Texture2D(width,height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode; canvas.worldCamera=oldCamera; canvas.planeDistance=oldDistance;
                camera.targetTexture=oldTarget; camera.aspect=oldAspect; RenderTexture.active=oldActive;
                RenderTexture.ReleaseTemporary(target); if(texture!=null) Destroy(texture);
                Canvas.ForceUpdateCanvases(); GetComponent<BattleView>().SendMessage("FitContent"); Canvas.ForceUpdateCanvases();
            }
        }

        private void OnAttack(BattleAttack attack)
        {
            attacks++;
        }

        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }

        private void OnDestroy()
        {
            if (!active) return;
            Application.logMessageReceived -= OnLog;
            if (battle != null) battle.Attacked -= OnAttack;
        }
    }
}
#endif

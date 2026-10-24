using UnityEngine;
namespace Riftrail
{
    public sealed class AutoBattleController : MonoBehaviour
    {
        public BattleSimulation Battle { get; private set; }
        public bool HasChosenHero { get; private set; }
        private BattleView view;
        private GameAudio gameAudio;
        private bool persistProgress;
        private long savedRevision=-1;
        private float nextSaveTime;
        private string progressKey=GameSaveStore.DefaultKey;
        private bool isolatedStore;
        private bool automatedSession, away, appFocused=true, appPaused;
        private void Awake()
        {
            var automated = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            automated = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-riftrail-smoke") >= 0;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var testArgs=System.Environment.GetCommandLineArgs();var testIndex=System.Array.IndexOf(testArgs,"-riftrail-persistence-test");
            if(testIndex>=0)
            {
                if(testIndex+3>=testArgs.Length || !testArgs[testIndex+2].StartsWith("Riftrail.Test.",System.StringComparison.Ordinal))throw new System.ArgumentException("Invalid isolated persistence test arguments.");
                progressKey=testArgs[testIndex+2];isolatedStore=true;automated=true;
            }
#endif
            automatedSession=automated;
            RebirthProgress progress=null;GameSave saved=null;
            persistProgress=!automated || isolatedStore;
            if(persistProgress && GameSaveStore.HasSave(progressKey))
            {
                if(!GameSaveStore.TryLoad(out saved,progressKey)) {persistProgress=false;Debug.LogWarning("Game save could not be read; the original is preserved.");}
            }
            else if(persistProgress && !isolatedStore && RebirthSaveStore.HasSave() && !RebirthSaveStore.TryLoad(out progress))
            {persistProgress=false;Debug.LogWarning("Legacy save could not be read; the original is preserved.");}
            Battle=saved!=null?BattleSimulation.Restore(saved):new BattleSimulation(progress);
            HasChosenHero=saved!=null;
            if(saved!=null)Battle.AccrueOffline(UtcNow());
            Battle.PermanentProgressChanged+=SaveProgress;
            Battle.IsPaused=!HasChosenHero;
            gameAudio=gameObject.AddComponent<GameAudio>(); gameAudio.Initialize(!automated);
            Battle.Attacked+=gameAudio.OnAttack; Battle.RelicFound+=gameAudio.OnRelic;
            view = gameObject.AddComponent<BattleView>();
            view.Initialize(Battle, ChallengeBoss, ResetAllProgress);
            Battle.Attacked += view.ShowAttack;
            Battle.GoldEarned += view.ShowGold;
            Battle.RelicFound+=view.ShowRelicReward;
            if(HasChosenHero){SaveProgressCore(true);if(Battle.PendingOfflineGold>0)view.ShowOfflineReward(ClaimOfflineReward);}
            if(!HasChosenHero)view.ShowHeroSelection(index =>
            {
                HasChosenHero = true;
                Battle.IsPaused = false;
                view.Render(Battle, false);
                SaveProgress();
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(isolatedStore)gameObject.AddComponent<GamePersistenceProbe>();else gameObject.AddComponent<BattleSmokeProbe>();
#endif
        }
        private void Update()
        {
            if (!away && !view.IsGrowthMenuOpen) Battle.Advance(Mathf.Min(Time.deltaTime, 0.25f));
            view.Render(Battle, !away && !view.IsGrowthMenuOpen);
            if(!away && Time.unscaledTime>=nextSaveTime) SaveProgress();
        }
        public void TogglePause() { if (!HasChosenHero || view.IsGrowthMenuOpen) return; view.CancelUpgradeHold(); Battle.IsPaused = !Battle.IsPaused; view.Render(Battle, false); }
        public void Upgrade() { if (view.IsGrowthMenuOpen) return; view.BuyGrowth(RebirthUpgrade.Attack); }
        public void ChallengeBoss() { if (!HasChosenHero || view.IsGrowthMenuOpen) return; if(Battle.CanAbandonBoss)Battle.TryAbandonBoss();else Battle.TryChallengeBoss(); view.Render(Battle, false);SaveProgress(); }
        private void ResetAllProgress()
        {
            HasChosenHero=false;view.PrepareFullReset();
            if(!automatedSession || isolatedStore)
            {
                GameSaveStore.DeleteProgress(progressKey,isolatedStore?progressKey+".Legacy":RebirthSaveStore.DefaultKey);
                if(!isolatedStore)PlayerPrefs.DeleteKey(GameAudio.PreferenceKey);
                PlayerPrefs.Save();persistProgress=true;
            }
            savedRevision=-1;Battle.Reset();Battle.IsPaused=true;
            gameAudio.SetSoundEnabled(false);gameAudio.SetSoundEnabled(true);
            view.ShowHeroSelection(index=>{HasChosenHero=true;Battle.IsPaused=false;view.Render(Battle,false);SaveProgress();});
            view.Render(Battle,false);
        }
        private void SaveProgress() => SaveProgressCore(false);
        private void SaveProgressCore(bool force)
        {
            if(!persistProgress || !HasChosenHero || Battle==null)return;
            var utc=UtcNow();
            if(!force && savedRevision==Battle.SaveRevision && utc-Battle.SavedUtcSeconds<30)return;
            if(!away)Battle.MarkSavedAt(utc);
            GameSaveStore.Save(Battle.ExportGame(),progressKey);savedRevision=Battle.SaveRevision;nextSaveTime=Time.unscaledTime+.5f;
        }
        internal void EndIsolatedSaveTest() {if(isolatedStore)persistProgress=false;}
        private static long UtcNow()=>System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        internal void ClaimOfflineReward()
        {
            if(!Battle.TryClaimOffline())return;
            SaveProgressCore(true);view.CloseOfflineReward();view.Render(Battle,false);
        }
        private void UpdateAwayState()
        {
            if(automatedSession)return;
            var shouldBeAway=!appFocused || appPaused;
#if UNITY_WEBGL && !UNITY_EDITOR
            // Keep browser focus changes online when background execution is enabled.
            shouldBeAway=appPaused || (!appFocused && !Application.runInBackground);
#endif
            if(shouldBeAway==away)return;
            if(shouldBeAway) {view?.CancelUpgradeHold();SaveProgressCore(true);away=true;return;}
            away=false;
            if(!HasChosenHero)return;
            Battle.AccrueOffline(UtcNow());SaveProgressCore(true);
            if(Battle.PendingOfflineGold>0)view.ShowOfflineReward(ClaimOfflineReward);
        }
        private void OnApplicationPause(bool paused) {appPaused=paused;UpdateAwayState();if(paused && automatedSession)SaveProgressCore(true);}
        private void OnApplicationFocus(bool focused) {appFocused=focused;UpdateAwayState();if(!focused && automatedSession)SaveProgressCore(true);}
        private void OnApplicationQuit() {SaveProgressCore(true);}
        private void OnDisable() { if (view != null) view.CancelUpgradeHold();SaveProgressCore(true); }
        private void OnDestroy()
        {
            if (Battle == null || view == null) return;
            SaveProgress();
            Battle.PermanentProgressChanged -= SaveProgress;
            Battle.Attacked -= view.ShowAttack;
            Battle.GoldEarned -= view.ShowGold;
            Battle.RelicFound-=view.ShowRelicReward;
            Battle.Attacked-=gameAudio.OnAttack; Battle.RelicFound-=gameAudio.OnRelic;
        }
    }
}

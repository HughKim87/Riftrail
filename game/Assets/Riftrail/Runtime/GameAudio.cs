using UnityEngine;
namespace Riftrail
{
    public sealed class GameAudio : MonoBehaviour
    {
        public const string PreferenceKey="Riftrail.Sound.v1";
        private AudioSource ui, attack, impact;
        private AudioClip click, hit, relic;
        private readonly AudioClip[] swings=new AudioClip[3];
        private bool persist;
        private string preferenceKey;
        public bool SoundEnabled {get;private set;}=true;
        public int PlayedClicks {get;private set;}
        public int PlayedAttacks {get;private set;}
        public int PlayedHits {get;private set;}
        public bool ClipsReady => click!=null && hit!=null && relic!=null && System.Array.TrueForAll(swings,c=>c!=null && c.samples>0);
        public void Initialize(bool persistSettings,string key=PreferenceKey)
        {
            persist=persistSettings;preferenceKey=key; SoundEnabled=!persist || PlayerPrefs.GetInt(preferenceKey,1)!=0;
            ui=Voice(); attack=Voice(); impact=Voice();
            click=Resources.Load<AudioClip>("Audio/Click"); hit=Resources.Load<AudioClip>("Audio/Hit"); relic=Resources.Load<AudioClip>("Audio/Relic");
            swings[0]=Resources.Load<AudioClip>("Audio/Sword"); swings[1]=Resources.Load<AudioClip>("Audio/Wuxia"); swings[2]=Resources.Load<AudioClip>("Audio/Ninja");
            if(FindFirstObjectByType<AudioListener>()==null) Camera.main.gameObject.AddComponent<AudioListener>();
            SetSoundEnabled(SoundEnabled);
        }
        private AudioSource Voice() { var source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0; source.volume=.55f; return source; }
        public void SetSoundEnabled(bool enabled)
        {
            SoundEnabled=enabled;
            foreach(var source in new[]{ui,attack,impact}) { source.mute=!enabled; if(!enabled) source.Stop(); }
            if(persist) {PlayerPrefs.SetInt(preferenceKey,enabled?1:0);PlayerPrefs.Save();}
        }
        public void Click() {if(!SoundEnabled)return; PlayedClicks++; ui.Stop();ui.PlayOneShot(click,.45f);}
        public void OnAttack(BattleAttack value)
        {
            if(!SoundEnabled)return;
            PlayedAttacks++; attack.Stop();attack.PlayOneShot(swings[(int)value.Hero],.7f);
            if(value.Damage>0) {PlayedHits++;impact.Stop();impact.PlayOneShot(hit,.65f);}
        }
        public void OnRelic(RelicKind kind) {if(SoundEnabled)ui.PlayOneShot(relic,.65f);}
    }
}

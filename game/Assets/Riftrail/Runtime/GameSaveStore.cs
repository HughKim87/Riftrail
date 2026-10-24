using System;
using UnityEngine;
namespace Riftrail
{
    /// <summary>Permanent and run progress are written together; legacy rebirth saves remain a migration source.</summary>
    public static class GameSaveStore
    {
        public const string DefaultKey="Riftrail.Game.v1";
        public static string Encode(GameSave save)
        {
            if(save==null || !save.IsValid)throw new ArgumentException("Invalid game save.");
            return JsonUtility.ToJson(save.ToCurrent());
        }
        public static bool TryDecode(string json,out GameSave save)
        {
            save=null;if(string.IsNullOrWhiteSpace(json))return false;
            try {var parsed=JsonUtility.FromJson<GameSave>(json);if(parsed==null || !parsed.IsValid)return false;save=parsed.ToCurrent();return true;}
            catch(ArgumentException){return false;}
        }
        public static void DeleteProgress(string currentKey=DefaultKey,string legacyKey=RebirthSaveStore.DefaultKey)
        {PlayerPrefs.DeleteKey(currentKey);PlayerPrefs.DeleteKey(legacyKey);PlayerPrefs.Save();}
        public static bool HasSave(string key=DefaultKey)=>PlayerPrefs.HasKey(key);
        public static bool TryLoad(out GameSave save,string key=DefaultKey)
        {save=null;return PlayerPrefs.HasKey(key) && TryDecode(PlayerPrefs.GetString(key),out save);}
        public static void Save(GameSave save,string key=DefaultKey)
        {PlayerPrefs.SetString(key,Encode(save));PlayerPrefs.Save();}
    }
}

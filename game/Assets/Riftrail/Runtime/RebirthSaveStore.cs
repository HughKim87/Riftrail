using System;
using UnityEngine;

namespace Riftrail
{
    /// <summary>Only permanent rebirth growth is persisted. Run combat remains session-only.</summary>
    public static class RebirthSaveStore
    {
        public const string DefaultKey = "Riftrail.Rebirth.v1";
        public static string Encode(RebirthProgress progress)
        {
            if (progress == null || !progress.IsValid) throw new ArgumentException("Invalid rebirth progress.");
            return JsonUtility.ToJson(progress);
        }
        public static bool TryDecode(string json, out RebirthProgress progress)
        {
            progress = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var decoded = JsonUtility.FromJson<RebirthProgress>(json);
                if (decoded == null || !decoded.IsValid) return false;
                progress = decoded;
                return true;
            }
            catch (ArgumentException) { return false; }
        }
        public static bool TryLoad(out RebirthProgress progress, string key = DefaultKey)
        {
            progress = null;
            return PlayerPrefs.HasKey(key) && TryDecode(PlayerPrefs.GetString(key), out progress);
        }
        public static bool HasSave(string key = DefaultKey) => PlayerPrefs.HasKey(key);
        public static void Save(RebirthProgress progress, string key = DefaultKey)
        {
            PlayerPrefs.SetString(key, Encode(progress));
            PlayerPrefs.Save();
        }
    }
}

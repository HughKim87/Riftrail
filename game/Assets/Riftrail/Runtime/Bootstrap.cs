using UnityEngine;

namespace Riftrail
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            gameObject.AddComponent<AutoBattleController>();
            Debug.Log("RIFTRAIL bootstrap ready.");
        }
    }
}

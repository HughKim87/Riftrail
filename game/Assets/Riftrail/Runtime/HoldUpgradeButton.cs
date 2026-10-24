using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Riftrail
{
    /// <summary>Mouse/touch press buys once, then repeats. Release never buys an extra level.</summary>
    public sealed class HoldUpgradeButton : Button
    {
        private const float InitialDelay = .35f;
        private int? pointerId;
        private PointerEventData pressedEvent;
        private float elapsed, nextRepeat;
        public bool IsHolding => pointerId.HasValue;

        public override void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointerId.HasValue
                || !IsActive() || !IsInteractable()) return;
            base.OnPointerDown(data);
            pointerId = data.pointerId;
            pressedEvent = data;
            elapsed = 0;
            nextRepeat = InitialDelay;
            onClick.Invoke();
        }
        public override void OnPointerUp(PointerEventData data)
        {
            if (pointerId != data.pointerId) return;
            CancelHold();
        }
        public override void OnPointerExit(PointerEventData data)
        {
            if (pointerId == data.pointerId) CancelHold();
            base.OnPointerExit(data);
        }
        public override void OnPointerClick(PointerEventData data)
        {
            // Pointer-down already purchased. Button.OnSubmit still supports keyboard/controller activation.
        }
        private void Update()
        {
            if (!IsHolding) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < nextRepeat) return;
            // No catch-up burst after a hitch. At most one purchase per rendered frame.
            nextRepeat = elapsed + (elapsed >= 1.2f ? .07f : .12f);
            // Gold shortage disables purchases but preserves the held gesture until release.
            if (IsActive() && IsInteractable()) onClick.Invoke();
        }
        public void CancelHold()
        {
            if (pressedEvent != null) base.OnPointerUp(pressedEvent);
            pressedEvent = null;
            pointerId = null;
            elapsed = nextRepeat = 0;
        }
        protected override void OnDisable() { CancelHold(); base.OnDisable(); }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelHold(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelHold(); }
    }
}

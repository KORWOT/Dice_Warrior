using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class UIRoot : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform safeArea, screenLayer, popupLayer, overlayLayer, cacheLayer;
        public CanvasGroup inputGroup;
        public Image popupBlocker;

        public void ApplySafeArea()
        {
            if (!canvas || !safeArea || !screenLayer || !popupLayer || !overlayLayer ||
                !cacheLayer || !inputGroup || !popupBlocker)
                throw new InvalidOperationException("UIRoot requires its authored canvas, layers, input group and popup blocker.");
            if (popupBlocker.transform.parent != popupLayer)
                throw new InvalidOperationException("The popup blocker must be a direct child of the popup layer.");
            if (Screen.width > 0 && Screen.height > 0)
            {
                Rect area = Screen.safeArea;
                safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
                safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            cacheLayer.gameObject.SetActive(false);
            var blocker = popupBlocker.rectTransform;
            blocker.anchorMin = Vector2.zero;
            blocker.anchorMax = Vector2.one;
            blocker.offsetMin = blocker.offsetMax = Vector2.zero;
            popupBlocker.raycastTarget = true;
        }
    }
}

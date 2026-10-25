using HarmonyLib;
using System.Reflection;
using TabInfo.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>
    /// The "Original TabInfo" theme: TabInfo's own window is shown untouched, plus a small row of
    /// TabStyle layout buttons in its title bar so you can switch back.
    /// </summary>
    internal static class OriginalMode
    {
        private static FieldInfo tabFrameField;
        private static FieldInfo toggledField;

        private static void Init()
        {
            if (tabFrameField == null) tabFrameField = AccessTools.Field(typeof(TabInfoManager), "tabFrame");
            if (toggledField == null) toggledField = AccessTools.Field(typeof(TabFrame), "toggled");
        }

        internal static TabFrame CurrentFrame
        {
            get
            {
                Init();
                try
                {
                    var f = tabFrameField?.GetValue(null) as TabFrame;
                    return f != null ? f : null;   // Unity "destroyed" objects count as null
                }
                catch { return null; }
            }
        }

        internal static bool TabInfoOpen
        {
            get
            {
                var f = CurrentFrame;
                return f != null && f.gameObject.activeSelf;
            }
        }

        /// <summary>Opens TabInfo's window (only works while the Original theme is selected).</summary>
        internal static void OpenTabInfo()
        {
            try
            {
                if (!TabInfoOpen) TabInfoManager.ToggleTabFrame();
                Attach();
            }
            catch (System.Exception e) { TabStyle.Log.LogWarning("Couldn't open TabInfo's window: " + e.Message); }
        }

        /// <summary>Closes TabInfo's window the same way TabInfo does.</summary>
        internal static void CloseTabInfo()
        {
            Init();
            var f = CurrentFrame;
            if (f == null) return;
            try { toggledField?.SetValue(f, false); } catch { }
            f.gameObject.SetActive(false);
            Object.Destroy(f.gameObject);
        }

        /// <summary>Adds the switcher to TabInfo's window if it's open and doesn't have one yet.</summary>
        internal static void Attach()
        {
            var f = CurrentFrame;
            if (f == null || !f.gameObject.activeSelf) return;
            if (f.GetComponent<OriginalSwitcher>() != null) return;
            f.gameObject.AddComponent<OriginalSwitcher>();
        }
    }

    internal class OriginalSwitcher : MonoBehaviour
    {
        private static readonly Color Bg = new Color(0.16f, 0.16f, 0.16f, 0.92f);
        private static readonly Color Hover = new Color(0.32f, 0.32f, 0.32f, 1f);
        private static readonly Color Icon = new Color(0.86f, 0.86f, 0.86f, 1f);
        private static readonly Color ActiveIcon = new Color(1f, 0.62f, 0.35f, 1f);

        private void Start()
        {
            try { Build(); }
            catch (System.Exception e) { TabStyle.Log.LogWarning("Couldn't add theme buttons to TabInfo: " + e.Message); }
        }

        private void Build()
        {
            var bar = transform.Find("Title Bar") as RectTransform;
            if (bar == null) return;
            var close = bar.Find("Close") as RectTransform;

            float h = Mathf.Max(16f, bar.rect.height * 0.78f);
            float closeW = close != null ? close.rect.width : h;

            var seg = UIKit.Box(bar, Bg, Mathf.RoundToInt(h * 0.22f), "TabStyle Themes", true);
            var rt = seg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-(closeW + h * 0.4f), 0f);
            var fitter = seg.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            int pad = Mathf.RoundToInt(h * 0.1f);
            UIKit.HL(seg, pad, pad, pad, pad, pad, TextAnchor.MiddleCenter);

            float btnH = h - pad * 2f;
            foreach (var kind in Themes.Layouts)
            {
                var k = kind;
                bool active = k == LayoutKind.Original;
                var btn = UIKit.Box(seg.transform, active ? Hover : new Color(0, 0, 0, 0), Mathf.RoundToInt(btnH * 0.2f), "Layout " + k, true);
                UIKit.LE(btn, btnH * 1.25f, btnH * 1.25f, 0, btnH, btnH, 0);
                var icon = UIKit.Rect(btn.transform, "Icon");
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(btnH * 0.75f, btnH * 0.53f);
                TabStyleUI.LayoutIcon(icon, k, active ? ActiveIcon : Icon);
                if (active) continue;
                UIKit.MakeButton(btn, () =>
                {
                    var ui = TabStyleUI.Instance;
                    if (ui != null) ui.SwitchFromOriginal(k);
                }, btn.color, Hover);
            }
            seg.transform.SetAsLastSibling();
        }
    }
}

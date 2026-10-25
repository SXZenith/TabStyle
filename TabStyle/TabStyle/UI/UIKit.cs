using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>Small helpers for building uGUI from code.</summary>
    internal static class UIKit
    {
        internal static TMP_FontAsset Font;

        // ---------------------------------------------------------------- sprites
        private static readonly Dictionary<int, Sprite> roundedCache = new Dictionary<int, Sprite>();

        /// <summary>A 9-sliced rounded-rectangle sprite with the given corner radius (in UI units).</summary>
        internal static Sprite Rounded(int radius)
        {
            radius = Mathf.Clamp(radius, 1, 64);
            if (roundedCache.TryGetValue(radius, out var cached) && cached != null) return cached;

            int size = radius * 2 + 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "TabStyle_Rounded" + radius,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float dx = Mathf.Max(radius - px, 0f, px - (size - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (size - radius));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            float b = radius + 1;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprite.name = tex.name;
            roundedCache[radius] = sprite;
            return sprite;
        }

        // ---------------------------------------------------------------- objects
        internal static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        internal static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        internal static Image Box(Transform parent, Color color, int radius = 0, string name = "Box", bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (radius > 0)
            {
                img.sprite = Rounded(radius);
                img.type = Image.Type.Sliced;
                img.fillCenter = true;
            }
            return img;
        }

        internal static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool rich = true, string name = "Text")
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.richText = rich;
            t.text = text ?? "";
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            return t;
        }

        internal static LayoutElement LE(Component c, float minW = -1, float prefW = -1, float flexW = -1, float minH = -1, float prefH = -1, float flexH = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minW; le.preferredWidth = prefW; le.flexibleWidth = flexW;
            le.minHeight = minH; le.preferredHeight = prefH; le.flexibleHeight = flexH;
            return le;
        }

        internal static HorizontalLayoutGroup HL(Component c, float spacing, int padL = 0, int padR = 0, int padT = 0, int padB = 0,
            TextAnchor align = TextAnchor.MiddleLeft, bool expandW = false, bool expandH = false)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = expandH;
            return g;
        }

        internal static VerticalLayoutGroup VL(Component c, float spacing, int padL = 0, int padR = 0, int padT = 0, int padB = 0,
            TextAnchor align = TextAnchor.UpperLeft, bool expandW = true, bool expandH = false)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = expandH;
            return g;
        }

        /// <summary>A flexible empty element that pushes siblings apart.</summary>
        internal static void Spacer(Transform parent, float flexW = 1, float flexH = 0)
        {
            var rt = Rect(parent, "Spacer");
            LE(rt, minW: 0, flexW: flexW, minH: 0, flexH: flexH);
        }

        /// <summary>Rounded box with a colored border. Returns the inner box (put content there).</summary>
        /// <remarks>
        /// The outer box carries a layout group, so the inner box's own layout still reports its size
        /// upward and the bordered box sizes itself like any other layout child.
        /// </remarks>
        internal static Image Bordered(Transform parent, Color fill, Color border, int radius, int thickness, string name = "Card")
        {
            var outer = Box(parent, border, radius, name, true);
            HL(outer, 0, thickness, thickness, thickness, thickness, TextAnchor.UpperLeft, true, true);
            // No flexible size on the inner box: the outer group already force-expands it to fill.
            // (Giving it flex here leaked "stretch me" up to rows like the header, which then
            // swallowed half the window.)
            return Box(outer.transform, fill, Mathf.Max(1, radius - thickness), "Inner");
        }

        /// <summary>Makes any graphic clickable with hover feedback.</summary>
        internal static Button MakeButton(Graphic target, Action onClick, Color normal, Color hover, Action onEnter = null, Action onExit = null)
        {
            target.raycastTarget = true;
            var btn = target.gameObject.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            btn.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                TabStyleUI.PlayClick();
                try { onClick?.Invoke(); } catch (Exception e) { TabStyle.Log.LogError(e); }
            });
            var tint = target.gameObject.AddComponent<HoverTint>();
            tint.Init(target, normal, hover, onEnter, onExit);
            return btn;
        }

        // ---------------------------------------------------------------- shapes
        /// <summary>A "V" chevron drawn from two bars. rotation: 0 = down, 90 = right(clockwise -90), 180 = up.</summary>
        internal static RectTransform Chevron(Transform parent, float size, Color color, float rotationZ)
        {
            var c = Rect(parent, "Chevron");
            LE(c, size, size, 0, size, size, 0);
            c.sizeDelta = new Vector2(size, size);
            var holder = Rect(c, "Rot");
            Stretch(holder);
            holder.localEulerAngles = new Vector3(0, 0, rotationZ);
            float len = size * 0.5f, thick = Mathf.Max(2f, size * 0.14f), off = size * 0.17f;
            for (int i = 0; i < 2; i++)
            {
                var bar = Box(holder, color, 1, "Bar");
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(len, thick);
                rt.anchoredPosition = new Vector2(i == 0 ? -off : off, 0f);
                rt.localEulerAngles = new Vector3(0, 0, i == 0 ? -45f : 45f);
            }
            return c;
        }

        internal static RectTransform Cross(Transform parent, float size, Color color)
        {
            var c = Rect(parent, "Cross");
            c.sizeDelta = new Vector2(size, size);
            for (int i = 0; i < 2; i++)
            {
                var bar = Box(c, color, 1, "Bar");
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size * 1.1f, Mathf.Max(2f, size * 0.13f));
                rt.localEulerAngles = new Vector3(0, 0, i == 0 ? 45f : -45f);
            }
            return c;
        }

        /// <summary>Places a child at fractional anchors inside parent (for little icons).</summary>
        internal static Image Block(Transform parent, Color color, float x0, float y0, float x1, float y1, int radius = 2)
        {
            var img = Box(parent, color, radius, "Block");
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        // ---------------------------------------------------------------- scrolling
        /// <summary>
        /// A scroll area. The returned content grows with its children.
        /// vertical: list that scrolls up/down. horizontal: strip that scrolls left/right.
        /// </summary>
        internal static RectTransform Scroll(Transform parent, bool vertical, bool horizontal, Color barColor, out SmartScroll scroll, string name = "Scroll")
        {
            var root = Rect(parent, name);
            var catcher = root.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);   // invisible, but catches wheel/drag over empty space
            catcher.raycastTarget = true;

            var viewport = Rect(root, "Viewport");
            Stretch(viewport, 0, vertical ? 10 : 0, 0, horizontal ? 7 : 0);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = vertical && !horizontal ? new Vector2(1, 1) : new Vector2(0, 1);
            if (horizontal && !vertical) { content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); }
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;

            scroll = root.gameObject.AddComponent<SmartScroll>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.vertical = vertical;
            scroll.horizontal = horizontal;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 60f;

            if (vertical)
            {
                scroll.verticalScrollbar = MakeBar(root, barColor, true);
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }
            if (horizontal)
            {
                scroll.horizontalScrollbar = MakeBar(root, barColor, false);
                scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }
            return content;
        }

        private static Scrollbar MakeBar(Transform parent, Color color, bool vertical)
        {
            var track = Box(parent, new Color(color.r, color.g, color.b, 0.25f), 3, vertical ? "VBar" : "HBar", true);
            var rt = track.rectTransform;
            if (vertical)
            {
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(5, 0); rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
                rt.sizeDelta = new Vector2(0, 4); rt.anchoredPosition = Vector2.zero;
            }
            var area = Rect(rt, "Area");
            Stretch(area);
            var handle = Box(area, color, 3, "Handle", true);
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;

            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = vertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;
            bar.transition = Selectable.Transition.None;
            var nav = bar.navigation; nav.mode = Navigation.Mode.None; bar.navigation = nav;
            return bar;
        }
    }

    /// <summary>
    /// ScrollRect that (1) turns the mouse wheel into sideways movement for horizontal strips,
    /// (2) hands the wheel to the parent list when it can't move any further, so a card strip
    /// never "traps" the wheel while you scroll down a list.
    /// </summary>
    internal class SmartScroll : ScrollRect
    {
        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive() || content == null || viewport == null)
            {
                ForwardToParent(data);
                return;
            }

            var d = data.scrollDelta;
            float wheel = Mathf.Abs(d.y) >= Mathf.Abs(d.x) ? d.y : -d.x;
            if (Mathf.Approximately(wheel, 0f)) return;

            var view = viewport.rect.size;
            var size = content.rect.size;
            float maxY = Mathf.Max(0f, size.y - view.y);
            float maxX = Mathf.Max(0f, size.x - view.x);
            var pos = content.anchoredPosition;
            var before = pos;

            bool useVertical = vertical && maxY > 0.5f;
            bool useHorizontal = horizontal && maxX > 0.5f;

            if (useVertical)
                pos.y = Mathf.Clamp(pos.y - wheel * scrollSensitivity, 0f, maxY);
            else if (useHorizontal)
                pos.x = Mathf.Clamp(pos.x + wheel * scrollSensitivity, -maxX, 0f);

            if ((pos - before).sqrMagnitude < 0.01f)
            {
                ForwardToParent(data);
                return;
            }

            StopMovement();
            content.anchoredPosition = pos;
        }

        private void ForwardToParent(PointerEventData data)
        {
            var parent = transform.parent;
            if (parent == null) return;
            var target = ExecuteEvents.GetEventHandler<IScrollHandler>(parent.gameObject);
            if (target != null) ExecuteEvents.Execute(target, data, ExecuteEvents.scrollHandler);
        }
    }

    /// <summary>Hover color change plus optional enter/exit callbacks.</summary>
    internal class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Graphic target;
        private Color normal, hover;
        private Action onEnter, onExit;

        internal void Init(Graphic g, Color n, Color h, Action enter, Action exit)
        {
            target = g; normal = n; hover = h; onEnter = enter; onExit = exit;
            if (target != null) target.color = normal;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (target != null) target.color = hover;
            try { onEnter?.Invoke(); } catch { }
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (target != null) target.color = normal;
            try { onExit?.Invoke(); } catch { }
        }

        private void OnDisable()
        {
            if (target != null) target.color = normal;
        }
    }
}

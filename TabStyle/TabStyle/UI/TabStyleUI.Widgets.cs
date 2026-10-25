using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>Reusable pieces shared by every layout.</summary>
    internal partial class TabStyleUI
    {
        private static readonly Color AvatarText = new Color(0.07f, 0.07f, 0.07f, 1f);

        internal static Color Alpha(Color c, float a) { c.a = a; return c; }

        // ------------------------------------------------------------------ panel shells
        private void Dim(float alpha)
        {
            var dim = UIKit.Box(content, new Color(0f, 0f, 0f, alpha), 0, "Dim");
            UIKit.Stretch(dim.rectTransform);
        }

        /// <summary>A centered window. Returns its inner box with a vertical layout (header goes in first).</summary>
        private RectTransform CenteredPanel(float maxW, float maxH, out float width, out float height)
        {
            Dim(0.35f);
            width = Mathf.Min(maxW, designSize.x - 80f);
            height = Mathf.Min(maxH, designSize.y - 70f);
            var inner = UIKit.Bordered(content, pal.Bg, pal.Line, 18, 1, "Panel");
            var outer = (RectTransform)inner.transform.parent;
            outer.anchorMin = outer.anchorMax = new Vector2(0.5f, 0.5f);
            outer.pivot = new Vector2(0.5f, 0.5f);
            outer.sizeDelta = new Vector2(width, height);
            outer.anchoredPosition = Vector2.zero;
            inner.raycastTarget = true;
            UIKit.VL(inner, 0, 0, 0, 0, 0);
            return inner.rectTransform;
        }

        private void Divider(Transform parent, bool vertical = false)
        {
            var d = UIKit.Box(parent, pal.Line, 0, "Divider");
            if (vertical) UIKit.LE(d, 1, 1, 0, 0, -1, 1);
            else UIKit.LE(d, 0, -1, 1, 1, 1, 0);
        }

        // ------------------------------------------------------------------ header
        private void Header(Transform parent, bool compact)
        {
            hintDefault = Themes.LayoutLabel(TabStyle.LayoutSetting.Value) + "  /  " + pal.Name;

            if (!compact)
            {
                var row = UIKit.Rect(parent, "Header");
                UIKit.HL(row, 14, 26, 18, 14, 14, TextAnchor.MiddleLeft);
                UIKit.LE(row, minH: 78, prefH: 78, flexH: 0);
                TitleBlock(row);
                UIKit.Spacer(row);
                hint = UIKit.Text(row, hintDefault, 15, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, false, "Hint");
                UIKit.LE(hint, 120, 240, 0);
                YouFirstToggle(row);
                CardsToggle(row);
                LayoutButtons(row);
                PaletteButtons(row);
                CloseButton(row);
            }
            else
            {
                var box = UIKit.Rect(parent, "Header");
                UIKit.VL(box, 12, 20, 14, 14, 14);
                UIKit.LE(box, flexH: 0);
                var top = UIKit.Rect(box, "Top");
                UIKit.HL(top, 10, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                TitleBlock(top);
                UIKit.Spacer(top);
                CloseButton(top);
                var toggles = UIKit.Rect(box, "Toggles");
                UIKit.HL(toggles, 10, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                YouFirstToggle(toggles);
                CardsToggle(toggles);
                UIKit.Spacer(toggles);
                hint = UIKit.Text(toggles, hintDefault, 14, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, false, "Hint");
                UIKit.LE(hint, 60, 160, 0);
                var bottom = UIKit.Rect(box, "Buttons");
                UIKit.HL(bottom, 10, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                LayoutButtons(bottom);
                UIKit.Spacer(bottom);
                PaletteButtons(bottom);
            }
        }

        private void TitleBlock(Transform parent)
        {
            var col = UIKit.Rect(parent, "Title");
            UIKit.VL(col, 2, 0, 0, 0, 0, TextAnchor.MiddleLeft, true);
            UIKit.LE(col, 150, -1, 0);
            var title = UIKit.Text(col, TitleText(), 28, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false, "Mode");
            title.characterSpacing = 4;
            Bind("title", title);
            var sub = UIKit.Text(col, SubtitleText(), 15, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false, "Sub");
            Bind("subtitle", sub);
        }

        private void SetHint(string s) { if (hint != null) hint.text = s; }
        private void ResetHint() { if (hint != null) hint.text = hintDefault; }

        private RectTransform Segment(Transform parent)
        {
            var seg = UIKit.Bordered(parent, pal.Surf, pal.Line, 10, 1, "Segment");
            UIKit.HL(seg, 2, 4, 4, 4, 4, TextAnchor.MiddleCenter);
            return seg.rectTransform;
        }

        private void LayoutButtons(Transform parent)
        {
            var seg = Segment(parent);
            foreach (var kind in Themes.Layouts)
            {
                var k = kind;
                bool active = TabStyle.LayoutSetting.Value == k;
                var btn = UIKit.Box(seg, active ? pal.Surf2 : Alpha(pal.Surf2, 0f), 7, "Layout " + k, true);
                UIKit.LE(btn, 40, 40, 0, 34, 34, 0);
                var icon = UIKit.Rect(btn.transform, "Icon");
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(24, 17);
                LayoutIcon(icon, k, active ? pal.Accent : pal.Muted);
                UIKit.MakeButton(btn, () => SetLayout(k), btn.color, active ? pal.Surf2 : Alpha(pal.Surf2, 0.7f),
                    () => SetHint(k == LayoutKind.Original ? "Original TabInfo" : Themes.LayoutLabel(k) + " layout"), ResetHint);
            }
        }

        internal static void LayoutIcon(RectTransform icon, LayoutKind k, Color c)
        {
            var dim = Alpha(c, 0.45f);
            switch (k)
            {
                case LayoutKind.Split:
                    UIKit.Block(icon, c, 0f, 0f, 0.34f, 1f);
                    UIKit.Block(icon, dim, 0.42f, 0f, 1f, 1f);
                    break;
                case LayoutKind.Compare:
                    UIKit.Block(icon, c, 0f, 0f, 0.28f, 1f);
                    UIKit.Block(icon, dim, 0.36f, 0f, 0.64f, 1f);
                    UIKit.Block(icon, dim, 0.72f, 0f, 1f, 1f);
                    break;
                case LayoutKind.Scoreboard:
                    UIKit.Block(icon, c, 0f, 0.7f, 1f, 1f, 1);
                    UIKit.Block(icon, dim, 0f, 0.35f, 1f, 0.62f, 1);
                    UIKit.Block(icon, dim, 0f, 0f, 1f, 0.27f, 1);
                    break;
                case LayoutKind.Dock:
                    UIKit.Block(icon, Alpha(c, 0.22f), 0f, 0f, 0.58f, 1f);
                    UIKit.Block(icon, c, 0.66f, 0f, 1f, 1f);
                    break;
                case LayoutKind.Classic:
                    UIKit.Block(icon, c, 0f, 0.72f, 1f, 1f, 1);
                    UIKit.Block(icon, dim, 0.2f, 0.38f, 1f, 0.6f, 1);
                    UIKit.Block(icon, dim, 0.2f, 0f, 1f, 0.26f, 1);
                    break;
                case LayoutKind.Original:
                    // Old-school square window with its orange team bar.
                    UIKit.Block(icon, Alpha(c, 0.3f), 0f, 0f, 1f, 1f, 0);
                    UIKit.Block(icon, Alpha(c, 0.75f), 0f, 0.8f, 1f, 1f, 0);
                    UIKit.Block(icon, new Color(0.86f, 0.42f, 0.2f, 1f), 0f, 0.5f, 1f, 0.72f, 0);
                    break;
            }
        }

        private void PaletteButtons(Transform parent)
        {
            var seg = Segment(parent);
            foreach (var p in Themes.Palettes)
            {
                var k = p.Kind;
                bool active = TabStyle.PaletteSetting.Value == k;
                var hit = UIKit.Box(seg, Alpha(pal.Surf2, 0f), 7, "Colors " + p.Name, true);
                UIKit.LE(hit, 32, 32, 0, 34, 34, 0);
                var ring = UIKit.Box(hit.transform, active ? pal.Text : Alpha(pal.Text, 0f), 14, "Ring");
                Center(ring.rectTransform, 28);
                var gap = UIKit.Box(ring.transform, pal.Surf, 12, "Gap");
                Center(gap.rectTransform, 24);
                var outer = UIKit.Box(gap.transform, p.Bg, 10, "Swatch");
                Center(outer.rectTransform, 20);
                var dot = UIKit.Box(outer.transform, p.Accent, 7, "Accent");
                Center(dot.rectTransform, 13);
                UIKit.MakeButton(hit, () => SetPalette(k), hit.color, Alpha(pal.Surf2, 0.8f),
                    () => SetHint(p.Name + " colors"), ResetHint);
            }
        }

        private static void Center(RectTransform rt, float size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
        }

        private void CardsToggle(Transform parent) =>
            Switch(parent, "CARDS", TabStyle.ShowCardsSetting.Value, ToggleCards, "Hide cards", "Show cards");

        private void YouFirstToggle(Transform parent) =>
            Switch(parent, "YOU FIRST", TabStyle.YouFirstSetting.Value, ToggleYouFirst,
                "Sort everyone normally", "Put you at the top / left");

        /// <summary>A labelled on/off pill. The whole pill is the click target.</summary>
        private void Switch(Transform parent, string text, bool on, System.Action toggle, string hintWhenOn, string hintWhenOff)
        {
            var pill = UIKit.Bordered(parent, pal.Surf, on ? pal.Accent : pal.Line, 10, 1, text + " Toggle");
            UIKit.HL(pill, 10, 14, 12, 4, 4, TextAnchor.MiddleCenter);
            var outer = pill.transform.parent.GetComponent<Image>();
            UIKit.LE(outer, -1, -1, 0, 42, 42, 0);
            var label = UIKit.Text(pill.transform, text, 14, on ? pal.Text : pal.Muted, FontStyles.Bold, TextAlignmentOptions.Midline, false, "Label");
            label.characterSpacing = 3;
            var track = UIKit.Box(pill.transform, on ? pal.Accent : pal.Line, 9, "Track");
            UIKit.LE(track, 34, 34, 0, 18, 18, 0);
            var knob = UIKit.Box(track.transform, on ? pal.AccText : pal.Muted, 7, "Knob");
            var krt = knob.rectTransform;
            krt.anchorMin = krt.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
            krt.pivot = new Vector2(on ? 1f : 0f, 0.5f);
            krt.sizeDelta = new Vector2(12, 12);
            krt.anchoredPosition = new Vector2(on ? -3f : 3f, 0f);
            UIKit.MakeButton(outer, toggle, outer.color, on ? pal.Accent : pal.Muted,
                () => SetHint(on ? hintWhenOn : hintWhenOff), ResetHint);
        }

        private void CloseButton(Transform parent)
        {
            var btn = UIKit.Box(parent, Alpha(pal.Surf2, 0f), 8, "Close", true);
            UIKit.LE(btn, 40, 40, 0, 40, 40, 0);
            var x = UIKit.Cross(btn.transform, 16, pal.Muted);
            x.anchorMin = x.anchorMax = new Vector2(0.5f, 0.5f);
            x.anchoredPosition = Vector2.zero;
            UIKit.MakeButton(btn, Close, btn.color, pal.Surf2, () => SetHint("Close  (Tab)"), ResetHint);
        }

        // ------------------------------------------------------------------ player bits
        private void Avatar(Transform parent, PlayerInfo p, float size)
        {
            var box = UIKit.Box(parent, p.TeamColor, Mathf.RoundToInt(size * 0.22f), "Avatar");
            UIKit.LE(box, size, size, 0, size, size, 0);
            var t = UIKit.Text(box.transform, p.Initial, size * 0.52f, AvatarText, FontStyles.Bold, TextAlignmentOptions.Center, false, "Initial");
            UIKit.Stretch(t.rectTransform);
        }

        private void Tag(Transform parent, PlayerInfo p, float size)
        {
            if (string.IsNullOrEmpty(p.Tag)) return;
            var box = UIKit.Box(parent, pal.Accent, 5, "Tag");
            UIKit.HL(box, 0, 7, 7, 2, 2, TextAnchor.MiddleCenter);
            UIKit.LE(box, -1, -1, 0);
            var t = UIKit.Text(box.transform, p.Tag, size, pal.AccText, FontStyles.Bold, TextAlignmentOptions.Center, false, "TagText");
            t.characterSpacing = 3;
        }

        /// <summary>Name + tag on one line. The name shrinks with "..." before the tag is ever pushed out.</summary>
        private void NameLine(Transform parent, PlayerInfo p, float size)
        {
            var row = UIKit.Rect(parent, "NameLine");
            UIKit.HL(row, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(row, 0, -1, 1);
            var name = UIKit.Text(row, p.Name, size, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false, "Name");
            UIKit.LE(name, 30, -1, 0);
            name.GetComponent<LayoutElement>().flexibleWidth = 0;
            Tag(row, p, Mathf.Max(11f, size * 0.5f));
            UIKit.Spacer(row);
        }

        private void HpBar(Transform parent, PlayerInfo p, float height, TextMeshProUGUI text = null)
        {
            var track = UIKit.Box(parent, pal.Line, Mathf.Max(1, Mathf.RoundToInt(height / 2f)), "HpBar");
            UIKit.LE(track, 20, -1, 1, height, height, 0);
            var fill = UIKit.Box(track.transform, Themes.HpColor(p.HpPct), Mathf.Max(1, Mathf.RoundToInt(height / 2f)), "Fill");
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(p.HpPct, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            hpBinds.Add(new HpBind { Id = p.Id, Fill = rt, FillImage = fill, Text = text, Current = p.HpPct, Target = p.HpPct });
        }

        /// <summary>"HP .... 145 / 160" above a bar.</summary>
        private void HpBlock(Transform parent, PlayerInfo p, float barHeight, float textSize)
        {
            var col = UIKit.Rect(parent, "Hp");
            UIKit.VL(col, 6);
            var top = UIKit.Rect(col, "Top");
            UIKit.HL(top, 8);
            Label(top, "HP");
            UIKit.Spacer(top);
            var val = UIKit.Text(top, HpText(p), textSize, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight, false, "HpText");
            HpBar(col, p, barHeight, val);
        }

        private TextMeshProUGUI Label(Transform parent, string text, float size = 13f)
        {
            var t = UIKit.Text(parent, text.ToUpperInvariant(), size, pal.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false, "Label");
            t.characterSpacing = 4;
            return t;
        }

        private TextMeshProUGUI Muted(Transform parent, string text, float size, string bindKey = null)
        {
            var t = UIKit.Text(parent, text, size, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false, "Muted");
            if (bindKey != null) Bind(bindKey, t);
            return t;
        }

        // ------------------------------------------------------------------ stats
        private void StatLine(Transform parent, PlayerInfo p, string category, StatRow row, float size)
        {
            var line = UIKit.Rect(parent, "Stat");
            UIKit.HL(line, 12, 0, 0, 3, 3, TextAnchor.MiddleLeft);
            var n = UIKit.Text(line, row.Name, size, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true, "Name");
            UIKit.LE(n, 30, -1, 1);
            var v = UIKit.Text(line, row.Value, size, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight, true, "Value");
            UIKit.LE(v, 24, -1, 0);
            Bind(StatKey(p.Id, category, row.Name), v);
        }

        /// <summary>A collapsible stat section. Collapsing is shared across players and layouts.</summary>
        private void CategoryCard(Transform parent, PlayerInfo p, CategoryBlock block, Color fill, float textSize = 17f)
        {
            bool isCollapsed = collapsed.Contains(block.Name);
            var card = UIKit.Box(parent, fill, 12, "Category " + block.Name, true);
            UIKit.VL(card, 2, 16, 16, 6, isCollapsed ? 6 : 12);

            var head = UIKit.Box(card.transform, Alpha(fill, 0f), 6, "Head", true);
            UIKit.HL(head, 8, 0, 0, 8, 8, TextAnchor.MiddleLeft);
            var title = Label(head.transform, block.Name, 13);
            UIKit.LE(title, 20, -1, 1);
            Muted(head.transform, block.Stats.Count.ToString(), 13);
            UIKit.Chevron(head.transform, 14, pal.Muted, isCollapsed ? 90f : 0f);
            var name = block.Name;
            UIKit.MakeButton(head, () => ToggleCollapsed(name), head.color, Alpha(pal.Line, 0.35f),
                () => SetHint(isCollapsed ? "Expand section" : "Collapse section"), ResetHint);

            if (isCollapsed) return;
            foreach (var row in block.Stats) StatLine(card.transform, p, block.Name, row, textSize);
        }

        /// <summary>Spreads sections across N equal columns, keeping them balanced in height.</summary>
        private void CategoryGrid(Transform parent, PlayerInfo p, List<CategoryBlock> blocks, int columns, Color fill)
        {
            if (blocks.Count == 0)
            {
                Muted(parent, "No stats to show. Turn stats on in TabInfo's settings (main menu > Mod Options).", 16);
                return;
            }
            columns = Mathf.Clamp(columns, 1, Mathf.Max(1, blocks.Count));
            var grid = UIKit.Rect(parent, "Grid");
            UIKit.HL(grid, 16, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var cols = new List<RectTransform>();
            var heights = new float[columns];
            for (int i = 0; i < columns; i++)
            {
                var c = UIKit.Rect(grid, "Column");
                UIKit.VL(c, 14);
                UIKit.LE(c, 0, 0, 1);
                cols.Add(c);
            }
            foreach (var b in blocks)
            {
                int best = 0;
                for (int i = 1; i < columns; i++) if (heights[i] < heights[best] - 0.01f) best = i;
                CategoryCard(cols[best], p, b, fill);
                heights[best] += 2.5f + (collapsed.Contains(b.Name) ? 0 : b.Stats.Count);
            }
        }

        // ------------------------------------------------------------------ quick stats
        private List<KeyValuePair<string, string>> PresentQuickStats(IEnumerable<KeyValuePair<string, string>> wanted, IEnumerable<PlayerInfo> players)
        {
            var list = players.ToList();
            return wanted.Where(w => list.Any(p => p.Basic.ContainsKey(w.Key))).ToList();
        }

        private void StatTiles(Transform parent, PlayerInfo p, List<KeyValuePair<string, string>> stats, int perRow, float valueSize, Color fill)
        {
            if (stats.Count == 0) return;
            var basic = TabInfoBridge.BasicCategoryName;
            var col = UIKit.Rect(parent, "Tiles");
            UIKit.VL(col, 8);
            for (int i = 0; i < stats.Count; i += perRow)
            {
                var row = UIKit.Rect(col, "Row");
                UIKit.HL(row, 8, 0, 0, 0, 0, TextAnchor.UpperLeft);
                for (int j = 0; j < perRow; j++)
                {
                    if (i + j >= stats.Count)
                    {
                        var filler = UIKit.Rect(row, "Filler");
                        UIKit.LE(filler, 0, 0, 1);
                        continue;
                    }
                    var s = stats[i + j];
                    var tile = UIKit.Box(row, fill, 10, "Tile");
                    UIKit.VL(tile, 2, 12, 10, 9, 9);
                    UIKit.LE(tile, 0, 0, 1);
                    Label(tile.transform, s.Value, 11);
                    p.Basic.TryGetValue(s.Key, out var value);
                    var v = UIKit.Text(tile.transform, value ?? "-", valueSize, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, true, "Value");
                    Bind(StatKey(p.Id, basic, s.Key), v);
                }
            }
        }

        // ------------------------------------------------------------------ cards
        private void CardsSection(Transform parent, PlayerInfo p, string scrollKey, bool withLabel = true)
        {
            if (!TabStyle.ShowCardsSetting.Value) return;
            var col = UIKit.Rect(parent, "Cards");
            UIKit.VL(col, 8);
            if (withLabel) Label(col, "Cards   " + p.Cards.Count);
            if (p.Cards.Count == 0)
            {
                Muted(col, "No cards yet", 15);
                return;
            }
            var strip = UIKit.Scroll(col, false, true, pal.Muted, out var sr, "Strip");
            UIKit.LE(strip.parent.parent, 0, -1, 1, 46, 46, 0);
            UIKit.HL(strip, 6, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            scrolls["cards|" + scrollKey] = sr;
            foreach (var card in p.Cards) CardChipView(strip, card);
        }

        private void CardChipView(Transform parent, CardInfo card)
        {
            var chip = UIKit.Bordered(parent, pal.Surf2, pal.Line, 8, 1, "Card");
            var outer = chip.transform.parent;
            UIKit.LE(outer.GetComponent<Image>(), -1, -1, 0, 36, 36, 0);
            UIKit.HL(chip, 8, 11, 12, 0, 0, TextAnchor.MiddleLeft);
            var dot = UIKit.Box(chip.transform, RarityColor(card), 5, "Rarity");
            UIKit.LE(dot, 9, 9, 0, 9, 9, 0);
            string cardName = "?";
            try { cardName = string.IsNullOrEmpty(card.cardName) ? card.name : card.cardName; } catch { }
            var t = UIKit.Text(chip.transform, cardName, 16, pal.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, false, "Name");
            UIKit.LE(t, -1, -1, 0);
            var hover = outer.gameObject.AddComponent<CardChip>();
            hover.Card = card;
            var tint = outer.gameObject.AddComponent<HoverTint>();
            tint.Init(outer.GetComponent<Image>(), pal.Line, pal.Accent, null, null);
        }

        private static MethodInfo rarityDataMethod;
        private static bool rarityLookupDone;

        private Color RarityColor(CardInfo card)
        {
            int r = 0;
            try { r = (int)card.rarity; } catch { }
            switch (r)
            {
                case 0: return pal.Muted;
                case 1: return Palette.Hex("#4C9AFF");
                case 2: return Palette.Hex("#E05CC8");
            }
            // Custom rarity from RarityLib (if installed).
            try
            {
                if (!rarityLookupDone)
                {
                    rarityLookupDone = true;
                    var t = AccessToolsType("RarityLib.Utils.RarityUtils");
                    rarityDataMethod = t?.GetMethod("GetRarityData", BindingFlags.Public | BindingFlags.Static);
                }
                if (rarityDataMethod != null)
                {
                    var data = rarityDataMethod.Invoke(null, new object[] { card.rarity });
                    var f = data?.GetType().GetField("color") ?? null;
                    if (f != null && f.GetValue(data) is Color c) { c.a = 1f; return c; }
                }
            }
            catch { }
            return pal.Accent;
        }

        private static Type AccessToolsType(string name)
        {
            try { return HarmonyLib.AccessTools.TypeByName(name); } catch { return null; }
        }

        /// <summary>A vertical scroll list filling the parent. Returns its content (vertical layout).</summary>
        private RectTransform VList(Transform parent, string key, int padL, int padR, int padT, int padB, float spacing)
        {
            var c = UIKit.Scroll(parent, true, false, pal.Muted, out var sr, "List");
            UIKit.LE(sr, 0, -1, 1, 0, -1, 1);
            UIKit.VL(c, spacing, padL, padR, padT, padB);
            scrolls[key] = sr;
            return c;
        }
    }
}

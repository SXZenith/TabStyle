using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>The four layouts. Each one only arranges the shared widgets differently.</summary>
    internal partial class TabStyleUI
    {
        // ================================================================== SPLIT: list on the left, details on the right
        private void BuildSplit()
        {
            var panel = CenteredPanel(1720f, 960f, out _, out _);
            Header(panel, false);
            Divider(panel);

            var body = UIKit.Rect(panel, "Body");
            UIKit.HL(body, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, false, true);
            UIKit.LE(body, 0, -1, 1, 0, -1, 1);

            // Left: everyone.
            var left = UIKit.Rect(body, "Players");
            UIKit.LE(left, 420, 420, 0, 0, -1, 1);
            UIKit.VL(left, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, true, true);
            var list = VList(left, "split.list", 14, 8, 14, 14, 4);
            if (snap.Grouped)
            {
                foreach (var team in snap.Teams)
                {
                    TeamHeader(list, team);
                    foreach (var p in team.Players) SplitRow(list, p);
                }
            }
            else
            {
                foreach (var p in snap.Players) SplitRow(list, p);
            }
            var tip = Muted(list, "Click a player to see their stats", 14);
            tip.margin = new Vector4(10, 10, 0, 0);

            Divider(body, true);

            // Right: the selected player.
            var right = UIKit.Rect(body, "Details");
            UIKit.LE(right, 0, -1, 1, 0, -1, 1);
            UIKit.VL(right, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, true, true);
            var detail = VList(right, "split.detail|" + selectedId, 30, 22, 26, 30, 22);
            var sel = snap.Find(selectedId) ?? snap.FirstLocal;
            if (sel == null) return;

            var head = UIKit.Rect(detail, "Head");
            UIKit.HL(head, 18, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            Avatar(head, sel, 72);
            var who = UIKit.Rect(head, "Who");
            UIKit.VL(who, 6, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(who, 120, -1, 1);
            NameLine(who, sel, 38);
            Muted(who, TeamLine(sel), 17, "pteam|" + sel.Id);
            var hp = UIKit.Rect(head, "HpHolder");
            UIKit.VL(hp, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(hp, 300, 320, 0);
            HpBlock(hp, sel, 10, 20);

            CardsSection(detail, sel, "split|" + sel.Id);
            CategoryGrid(detail, sel, sel.Categories, 3, pal.Surf);
        }

        private void TeamHeader(Transform parent, TeamInfo team)
        {
            var row = UIKit.Rect(parent, "Team");
            UIKit.HL(row, 10, 10, 10, 12, 4, TextAnchor.MiddleLeft);
            var dot = UIKit.Box(row, team.Color, 5, "Dot");
            UIKit.LE(dot, 10, 10, 0, 10, 10, 0);
            var name = Label(row, team.Name, 13);
            UIKit.LE(name, 20, -1, 1);
            var score = Muted(row, ScoreText(team.Rounds, team.Points, false), 13, "tscore|" + team.Team);
            score.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void SplitRow(Transform parent, PlayerInfo p)
        {
            bool selected = p.Id == selectedId;
            var inner = UIKit.Bordered(parent, selected ? pal.Surf2 : pal.Bg, selected ? pal.Accent : pal.Bg, 12, 2, "Player " + p.Id);
            UIKit.HL(inner, 14, 12, 14, 10, 10, TextAnchor.MiddleLeft);
            Avatar(inner.transform, p, 46);

            var mid = UIKit.Rect(inner.transform, "Mid");
            UIKit.VL(mid, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(mid, 60, -1, 1);
            NameLine(mid, p, 21);
            HpBar(mid, p, 6);

            var right = UIKit.Rect(inner.transform, "Right");
            UIKit.VL(right, 4, 0, 0, 0, 0, TextAnchor.MiddleRight, true);
            UIKit.LE(right, 70, -1, 0);
            var cards = Muted(right, p.Cards.Count + (p.Cards.Count == 1 ? " card" : " cards"), 14);
            cards.alignment = TextAlignmentOptions.MidlineRight;
            if (!snap.Grouped)
            {
                var sc = Muted(right, ScoreText(p.Rounds, p.Points, true), 14, "pscore|" + p.Id);
                sc.alignment = TextAlignmentOptions.MidlineRight;
            }

            int id = p.Id;
            UIKit.MakeButton(inner, () => Select(id), inner.color, selected ? pal.Surf2 : pal.Surf);
        }

        // ================================================================== COMPARE: everyone side by side
        private void BuildCompare()
        {
            var panel = CenteredPanel(1860f, 1010f, out float panelW, out _);
            Header(panel, false);
            Divider(panel);

            var c = UIKit.Scroll(panel, true, true, pal.Muted, out var sr, "Board");
            UIKit.LE(sr, 0, -1, 1, 0, -1, 1);
            scrolls["compare"] = sr;
            UIKit.HL(c, 14, 16, 16, 16, 20, TextAnchor.UpperLeft, false, false);

            int n = snap.Players.Count;
            float avail = panelW - 2f - 32f - 14f - 14f * (n - 1);
            float colW = n <= 4 ? Mathf.Floor(avail / Mathf.Max(1, n)) : 400f;
            colW = Mathf.Max(330f, colW);

            foreach (var p in snap.Players)
            {
                var col = UIKit.Bordered(c, pal.Surf, p.Local ? pal.Accent : pal.Line, 14, p.Local ? 2 : 1, "Column " + p.Id);
                UIKit.LE(col.transform.parent.GetComponent<Image>(), colW, colW, 0);
                UIKit.VL(col, 14, 18, 18, 16, 18);

                var head = UIKit.Rect(col.transform, "Head");
                UIKit.HL(head, 12, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                Avatar(head, p, 50);
                var who = UIKit.Rect(head, "Who");
                UIKit.VL(who, 4, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                UIKit.LE(who, 60, -1, 1);
                NameLine(who, p, 24);
                Muted(who, TeamLine(p), 14, "pteam|" + p.Id);

                HpBlock(col.transform, p, 7, 16);
                CardsSection(col.transform, p, "compare|" + p.Id);

                if (p.Categories.Count == 0) Muted(col.transform, "No stats to show", 15);
                foreach (var block in p.Categories) CategoryCard(col.transform, p, block, pal.Surf2, 16f);
            }
        }

        // ================================================================== SCOREBOARD: one row per player
        private const float SbHp = 230f, SbStat = 118f, SbCards = 120f;

        private void BuildScoreboard()
        {
            var panel = CenteredPanel(1860f, 1010f, out _, out _);
            Header(panel, false);
            Divider(panel);

            var quick = PresentQuickStats(Themes.QuickStats, snap.Players);

            // Column titles (stay put while the rows scroll).
            var titles = UIKit.Rect(panel, "Columns");
            // Left/right padding = list padding + row border + row padding (+ scrollbar gutter on the right) so titles sit over their columns.
            UIKit.HL(titles, 0, 36, 56, 14, 8, TextAnchor.MiddleLeft);
            var pt = Label(titles, "Player");
            UIKit.LE(pt, 280, -1, 1);
            var ht = Label(titles, "HP");
            UIKit.LE(ht, SbHp, SbHp, 0);
            foreach (var q in quick)
            {
                var t = Label(titles, q.Value);
                t.alignment = TextAlignmentOptions.MidlineRight;
                UIKit.LE(t, SbStat, SbStat, 0);
            }
            var ct = Label(titles, "Cards");
            ct.alignment = TextAlignmentOptions.MidlineRight;
            UIKit.LE(ct, SbCards, SbCards, 0);

            var list = VList(panel, "board", 18, 18, 4, 22, 6);
            if (snap.Grouped)
            {
                foreach (var team in snap.Teams)
                {
                    BoardTeamHeader(list, team);
                    foreach (var p in team.Players) BoardRow(list, p, quick);
                }
            }
            else
            {
                foreach (var p in snap.Players) BoardRow(list, p, quick);
            }
            var tip = Muted(list, "Click a row to show cards and every stat", 14);
            tip.margin = new Vector4(14, 10, 0, 0);
        }

        private void BoardTeamHeader(Transform parent, TeamInfo team)
        {
            var box = UIKit.Rect(parent, "Team");
            UIKit.VL(box, 6, 0, 0, 12, 4);
            var row = UIKit.Rect(box, "Row");
            UIKit.HL(row, 12, 14, 14, 0, 0, TextAnchor.MiddleLeft);
            var name = UIKit.Text(row, team.Name.ToUpperInvariant(), 18, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false, "Name");
            name.characterSpacing = 4;
            UIKit.LE(name, 40, -1, 0);
            var score = Muted(row, ScoreText(team.Rounds, team.Points, false), 15, "tscore|" + team.Team);
            UIKit.LE(score, 40, -1, 1);
            var line = UIKit.Box(box, team.Color, 1, "Line");
            UIKit.LE(line, 0, -1, 1, 2, 2, 0);
        }

        private void BoardRow(Transform parent, PlayerInfo p, List<KeyValuePair<string, string>> quick)
        {
            bool open = expanded.Contains(p.Id);
            var card = UIKit.Bordered(parent, p.Local ? pal.Surf2 : pal.Surf, p.Local ? pal.Accent : pal.Surf, 12, 2, "Row " + p.Id);
            UIKit.VL(card, 0);

            var main = UIKit.Box(card.transform, Alpha(pal.Line, 0f), 10, "Main", true);
            UIKit.HL(main, 0, 16, 26, 10, 10, TextAnchor.MiddleLeft);

            var who = UIKit.Rect(main.transform, "Who");
            UIKit.HL(who, 12, 0, 12, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(who, 280, -1, 1);
            Avatar(who, p, 38);
            NameLine(who, p, 20);
            if (!snap.Grouped) Muted(who, ScoreText(p.Rounds, p.Points, true), 14, "pscore|" + p.Id);

            var hp = UIKit.Rect(main.transform, "Hp");
            UIKit.HL(hp, 10, 0, 18, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(hp, SbHp, SbHp, 0);
            var hpText = UIKit.Text(hp, HpText(p), 15, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, false, "HpText");
            HpBar(hp, p, 6, hpText);
            UIKit.LE(hpText, 86, 86, 0);
            hpText.transform.SetAsLastSibling();

            var basic = TabInfoBridge.BasicCategoryName;
            foreach (var q in quick)
            {
                p.Basic.TryGetValue(q.Key, out var value);
                var v = UIKit.Text(main.transform, value ?? "-", 19, pal.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight, true, "Stat");
                UIKit.LE(v, SbStat, SbStat, 0);
                Bind(StatKey(p.Id, basic, q.Key), v);
            }

            var cards = UIKit.Rect(main.transform, "Cards");
            UIKit.HL(cards, 10, 0, 0, 0, 0, TextAnchor.MiddleRight);
            UIKit.LE(cards, SbCards, SbCards, 0);
            var count = Muted(cards, p.Cards.Count.ToString(), 17);
            count.alignment = TextAlignmentOptions.MidlineRight;
            UIKit.Chevron(cards, 14, pal.Muted, open ? 180f : 0f);

            int id = p.Id;
            UIKit.MakeButton(main, () => ToggleExpanded(id), main.color, Alpha(pal.Line, 0.35f),
                () => SetHint(open ? "Hide details" : "Show cards and every stat"), ResetHint);

            if (!open) return;

            var more = UIKit.Rect(card.transform, "Details");
            UIKit.VL(more, 14, 66, 22, 4, 18);
            CardsSection(more, p, "board|" + p.Id);

            // Everything not already shown as a column.
            var shown = new HashSet<string>(quick.Select(q => q.Key)) { "HP" };
            var blocks = new List<CategoryBlock>();
            foreach (var b in p.Categories)
            {
                if (!b.IsBasic) { blocks.Add(b); continue; }
                var rest = new CategoryBlock { Name = b.Name, IsBasic = true };
                rest.Stats.AddRange(b.Stats.Where(s => !shown.Contains(s.Name)));
                if (rest.Stats.Count > 0) blocks.Add(rest);
            }
            if (blocks.Count > 0) CategoryGrid(more, p, blocks, 3, p.Local ? pal.Surf : pal.Surf2);
        }

        // ================================================================== DOCK: slim panel on the right edge
        private void BuildDock()
        {
            float width = Mathf.Min(600f, designSize.x - 40f);
            var inner = UIKit.Bordered(content, pal.Bg, pal.Line, 18, 1, "Dock");
            var outer = (RectTransform)inner.transform.parent;
            outer.anchorMin = new Vector2(1f, 0f);
            outer.anchorMax = new Vector2(1f, 1f);
            outer.pivot = new Vector2(1f, 0.5f);
            outer.sizeDelta = new Vector2(width, -28f);
            outer.anchoredPosition = new Vector2(-14f, 0f);
            inner.raycastTarget = true;
            UIKit.VL(inner, 0);

            Header(inner.transform, true);
            Divider(inner.transform);

            var list = VList(inner.transform, "dock", 20, 14, 18, 24, 16);
            var me = snap.FirstLocal;
            var tiles = PresentQuickStats(Themes.QuickStats.Concat(Themes.DockExtraStats), snap.Players);

            if (me != null)
            {
                var head = UIKit.Rect(list, "Me");
                UIKit.HL(head, 14, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                Avatar(head, me, 58);
                var who = UIKit.Rect(head, "Who");
                UIKit.VL(who, 4, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                UIKit.LE(who, 60, -1, 1);
                NameLine(who, me, 28);
                Muted(who, TeamLine(me), 14, "pteam|" + me.Id);

                HpBlock(list, me, 10, 18);
                StatTiles(list, me, tiles, 4, 22, pal.Surf);
                CardsSection(list, me, "dock|" + me.Id);

                var allBtn = UIKit.Bordered(list, pal.Surf, pal.Line, 10, 1, "All Stats");
                UIKit.HL(allBtn, 8, 14, 14, 10, 10, TextAnchor.MiddleLeft);
                var lbl = Label(allBtn.transform, dockAllStats ? "Hide all stats" : "All stats", 13);
                UIKit.LE(lbl, 20, -1, 1);
                UIKit.Chevron(allBtn.transform, 14, pal.Muted, dockAllStats ? 180f : 0f);
                UIKit.MakeButton(allBtn, ToggleDockAllStats, allBtn.color, pal.Surf2);

                if (dockAllStats)
                {
                    if (me.Categories.Count == 0) Muted(list, "No stats to show", 15);
                    foreach (var b in me.Categories) CategoryCard(list, me, b, pal.Surf);
                }
            }

            var others = snap.Players.Where(p => me == null || p.Id != me.Id).ToList();
            if (others.Count == 0) return;

            var otherTitle = Label(list, "Other players", 13);
            otherTitle.margin = new Vector4(4, 8, 0, 0);
            foreach (var p in others) DockRow(list, p, tiles);
        }

        private void DockRow(Transform parent, PlayerInfo p, List<KeyValuePair<string, string>> tiles)
        {
            bool open = dockOpenId == p.Id;
            var box = UIKit.Box(parent, open ? pal.Surf : Alpha(pal.Surf, 0f), 12, "Player " + p.Id, true);
            UIKit.VL(box, 0);

            var main = UIKit.Box(box.transform, Alpha(pal.Surf2, 0f), 12, "Main", true);
            UIKit.HL(main, 12, 10, 14, 9, 9, TextAnchor.MiddleLeft);
            Avatar(main.transform, p, 38);
            var who = UIKit.Rect(main.transform, "Who");
            UIKit.VL(who, 2, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(who, 80, 170, 0);
            NameLine(who, p, 18);
            Muted(who, snap.Grouped ? p.TeamName : ScoreText(p.Rounds, p.Points, true), 13, snap.Grouped ? null : "pscore|" + p.Id);
            HpBar(main.transform, p, 6);
            var cards = Muted(main.transform, p.Cards.Count + (p.Cards.Count == 1 ? " card" : " cards"), 14);
            cards.alignment = TextAlignmentOptions.MidlineRight;
            UIKit.LE(cards, 70, 70, 0);
            UIKit.Chevron(main.transform, 14, pal.Muted, open ? 180f : 0f);

            int id = p.Id;
            UIKit.MakeButton(main, () => ToggleDockOpen(id), main.color, Alpha(pal.Surf2, open ? 0.6f : 1f));

            if (!open) return;
            var more = UIKit.Rect(box.transform, "Details");
            UIKit.VL(more, 12, 14, 14, 2, 14);
            StatTiles(more, p, tiles, 4, 18, pal.Surf2);
            CardsSection(more, p, "dockrow|" + p.Id);
            foreach (var b in p.Categories) CategoryCard(more, p, b, pal.Surf2, 16f);
        }
    }
}

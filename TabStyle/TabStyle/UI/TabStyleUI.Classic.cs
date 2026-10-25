using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>
    /// CLASSIC: the same structure as the original TabInfo window (team banners, then each player
    /// with their cards and stat sections underneath, one long list), rebuilt in the modern style.
    /// </summary>
    internal partial class TabStyleUI
    {
        private void BuildClassic()
        {
            var panel = CenteredPanel(1560f, 1010f, out float panelW, out _);
            Header(panel, false);
            Divider(panel);

            var list = VList(panel, "classic", 22, 22, 18, 26, 12);
            int columns = panelW >= 1300f ? 4 : 3;

            foreach (var team in snap.Teams)
            {
                ClassicTeamBanner(list, team);
                foreach (var p in team.Players) ClassicPlayer(list, p, columns);
            }
        }

        private void ClassicTeamBanner(Transform parent, TeamInfo team)
        {
            var banner = UIKit.Box(parent, team.Color, 12, "Team " + team.Team);
            UIKit.HL(banner, 14, 22, 22, 12, 12, TextAnchor.MiddleLeft);
            var name = UIKit.Text(banner.transform, team.Name.ToUpperInvariant(), 26, AvatarText, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false, "Name");
            name.characterSpacing = 4;
            UIKit.LE(name, 60, -1, 1);
            var score = UIKit.Text(banner.transform, ScoreText(team.Rounds, team.Points, false), 18, AvatarText, FontStyles.Bold, TextAlignmentOptions.MidlineRight, false, "Score");
            UIKit.LE(score, 60, -1, 0);
            Bind("tscore|" + team.Team, score);
        }

        private void ClassicPlayer(Transform parent, PlayerInfo p, int columns)
        {
            bool open = !classicCollapsed.Contains(p.Id);
            var card = UIKit.Bordered(parent, pal.Surf, p.Local ? pal.Accent : pal.Line, 14, p.Local ? 2 : 1, "Player " + p.Id);
            UIKit.VL(card, 0);

            var head = UIKit.Box(card.transform, Alpha(pal.Surf2, 0f), 12, "Head", true);
            UIKit.HL(head, 16, 16, 20, 12, 12, TextAnchor.MiddleLeft);
            Avatar(head.transform, p, 48);
            NameLine(head.transform, p, 26);

            var hp = UIKit.Rect(head.transform, "Hp");
            UIKit.HL(hp, 12, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UIKit.LE(hp, 300, 300, 0);
            var hpText = UIKit.Text(hp, HpText(p), 16, pal.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, false, "HpText");
            HpBar(hp, p, 7, hpText);
            UIKit.LE(hpText, 96, 96, 0);
            hpText.transform.SetAsLastSibling();

            var count = Muted(head.transform, p.Cards.Count + (p.Cards.Count == 1 ? " card" : " cards"), 16);
            count.alignment = TextAlignmentOptions.MidlineRight;
            UIKit.LE(count, 80, 80, 0);
            UIKit.Chevron(head.transform, 16, pal.Muted, open ? 180f : 0f);

            int id = p.Id;
            UIKit.MakeButton(head, () => ToggleClassic(id), head.color, Alpha(pal.Surf2, 0.8f),
                () => SetHint(open ? "Collapse player" : "Expand player"), ResetHint);

            if (!open) return;

            var body = UIKit.Rect(card.transform, "Body");
            UIKit.VL(body, 14, 20, 20, 2, 20);
            CardsSection(body, p, "classic|" + p.Id);
            CategoryGrid(body, p, p.Categories, columns, pal.Surf2);
        }
    }
}

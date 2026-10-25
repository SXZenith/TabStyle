using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TabInfo.Utils;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;

namespace TabStyle
{
    internal sealed class StatRow
    {
        public string Name;
        public string Value;
    }

    internal sealed class CategoryBlock
    {
        public string Name;
        public bool IsBasic;
        public readonly List<StatRow> Stats = new List<StatRow>();
    }

    internal sealed class PlayerInfo
    {
        public Player Player;
        public int Id;
        public int Team;
        public string Name;
        public string Tag;          // "YOU", "P1", ... or null
        public bool Local;
        public float Hp, MaxHp;
        public Color TeamColor;
        public string TeamName;
        public int Rounds, Points;
        public readonly List<CardInfo> Cards = new List<CardInfo>();
        public readonly List<CategoryBlock> Categories = new List<CategoryBlock>();
        public readonly Dictionary<string, string> Basic = new Dictionary<string, string>();

        public float HpPct => MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f;
        public string Initial
        {
            get
            {
                foreach (var ch in Name ?? "")
                {
                    if (!char.IsLetterOrDigit(ch)) continue;
                    var up = char.ToUpperInvariant(ch);
                    // Skip letters the game font can't draw (they'd show as a box).
                    try { if (UIKit.Font != null && !UIKit.Font.HasCharacter(up)) continue; } catch { }
                    return up.ToString();
                }
                return "?";
            }
        }
    }

    internal sealed class TeamInfo
    {
        public int Team;
        public string Name;
        public Color Color;
        public int Rounds, Points;
        public readonly List<PlayerInfo> Players = new List<PlayerInfo>();
    }

    /// <summary>A read of the whole game state at one moment. Built a few times a second while open.</summary>
    internal sealed class Snapshot
    {
        public string ModeName = "";
        public int Round, Point, RoundsToWin, PointsToWin;
        public readonly List<PlayerInfo> Players = new List<PlayerInfo>();   // local players first, then by team/id
        public readonly List<TeamInfo> Teams = new List<TeamInfo>();
        public bool Grouped;    // true when at least one team has 2+ players
        public string StructureKey = "";
        public bool YouFirst;

        public PlayerInfo Find(int id)
        {
            foreach (var p in Players) if (p.Id == id) return p;
            return null;
        }

        public PlayerInfo FirstLocal => Players.FirstOrDefault(p => p.Local) ?? Players.FirstOrDefault();

        internal static Snapshot Take(bool youFirst)
        {
            var snap = new Snapshot { YouFirst = youFirst };
            var pm = PlayerManager.instance;
            if (pm == null || pm.players == null) return snap;

            var handler = SafeHandler();
            try { snap.ModeName = handler?.Name ?? ""; } catch { }
            try { snap.Round = TabInfoManager.CurrentRound; } catch { }
            try { snap.Point = TabInfoManager.CurrentPoint; } catch { }
            try { snap.RoundsToWin = TabInfoManager.RoundsToWin; } catch { }
            try { snap.PointsToWin = TabInfoManager.PointsToWin; } catch { }

            var players = pm.players.Where(p => p != null && p.data != null).ToList();

            // Who is "you": local players that a human controls. Bots owned by the host have no input actions.
            var locals = players.Where(IsLocal).ToList();
            var humans = locals.Where(p => p.data.playerActions != null).ToList();
            if (humans.Count > 0) locals = humans;

            var categories = TabInfoBridge.OrderedCategories();
            var statsPerCategory = categories.ToDictionary(c => c, TabInfoBridge.OrderedStats);
            string basicName = TabInfoBridge.BasicCategoryName;

            foreach (var player in players)
            {
                var info = new PlayerInfo
                {
                    Player = player,
                    Id = player.playerID,
                    Team = player.teamID,
                    Local = locals.Contains(player),
                };
                info.Name = PlayerName(player);
                try { info.Hp = player.data.health; info.MaxHp = player.data.maxHealth; } catch { }

                int colorId = player.teamID;
                try { colorId = player.colorID(); } catch { }
                info.TeamColor = TeamColor(colorId);
                info.TeamName = TeamName(colorId, player.teamID);

                if (handler != null)
                {
                    try { var s = handler.GetTeamScore(player.teamID); info.Rounds = s.rounds; info.Points = s.points; } catch { }
                }

                try
                {
                    if (player.data.currentCards != null)
                        info.Cards.AddRange(player.data.currentCards.Where(c => c != null));
                }
                catch { }

                foreach (var cat in categories)
                {
                    if (!TabInfoBridge.CategoryVisible(cat, player)) continue;
                    var block = new CategoryBlock { Name = cat.name ?? "Stats", IsBasic = cat.name == basicName };
                    foreach (var stat in statsPerCategory[cat])
                    {
                        if (!TabInfoBridge.StatVisible(stat, player)) continue;
                        var row = new StatRow { Name = stat.name ?? "", Value = TabInfoBridge.StatValue(stat, player) };
                        block.Stats.Add(row);
                        if (block.IsBasic) info.Basic[row.Name] = row.Value;
                    }
                    if (block.Stats.Count > 0) info.Categories.Add(block);
                }

                snap.Players.Add(info);
            }

            // Tags: one local human -> "YOU". Several on one PC (couch play) -> "P1", "P2"...
            var localInfos = snap.Players.Where(p => p.Local).ToList();
            foreach (var p in localInfos)
                p.Tag = localInfos.Count == 1 ? "YOU" : "P" + (p.Id + 1);

            snap.Players.Sort((a, b) =>
            {
                if (youFirst && a.Local != b.Local) return a.Local ? -1 : 1;
                if (a.Team != b.Team) return a.Team.CompareTo(b.Team);
                return a.Id.CompareTo(b.Id);
            });

            // Teams in team order, but the team holding a local player goes first.
            var localTeam = youFirst && localInfos.Count > 0 ? localInfos.Min(p => p.Team) : int.MinValue;
            foreach (var group in snap.Players.GroupBy(p => p.Team)
                         .OrderBy(g => g.Key == localTeam ? 0 : 1).ThenBy(g => g.Key))
            {
                var first = group.First();
                var team = new TeamInfo { Team = group.Key, Name = first.TeamName, Color = first.TeamColor, Rounds = first.Rounds, Points = first.Points };
                team.Players.AddRange(group.OrderBy(p => youFirst && p.Local ? 0 : 1).ThenBy(p => p.Id));
                snap.Teams.Add(team);
            }
            snap.Grouped = snap.Teams.Any(t => t.Players.Count > 1);

            snap.StructureKey = BuildKey(snap);
            return snap;
        }

        private static IGameModeHandler SafeHandler()
        {
            try { return GameModeManager.CurrentHandler; } catch { return null; }
        }

        private static bool IsLocal(Player p)
        {
            try { return PhotonNetwork.OfflineMode || (p.data.view != null && p.data.view.IsMine); }
            catch { return false; }
        }

        private static string PlayerName(Player p)
        {
            string name = null;
            try { name = p.data.view?.Owner?.NickName; } catch { }
            if (string.IsNullOrWhiteSpace(name)) name = "Player " + (p.playerID + 1);
            name = name.Trim();
            return name.Length > 32 ? name.Substring(0, 32) : name;
        }

        private static Color TeamColor(int colorId)
        {
            try
            {
                var skin = ExtraPlayerSkins.GetPlayerSkinColors(colorId);
                if (skin != null)
                {
                    var c = skin.winText;
                    c.a = 1f;
                    return c;
                }
            }
            catch { }
            return new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        private static string TeamName(int colorId, int teamId)
        {
            try
            {
                var n = ExtraPlayerSkins.GetTeamColorName(colorId);
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch { }
            return "Team " + (teamId + 1);
        }

        /// <summary>Everything that changes the shape of the UI (not just numbers in it).</summary>
        private static string BuildKey(Snapshot s)
        {
            var sb = new StringBuilder(256);
            sb.Append(s.Grouped ? 'G' : 'F').Append('|');
            foreach (var p in s.Players)
            {
                sb.Append(p.Id).Append(',').Append(p.Team).Append(',').Append(p.Name).Append(',').Append(p.Tag).Append(',');
                sb.Append(p.TeamName).Append(',').Append(ColorUtility.ToHtmlStringRGB(p.TeamColor)).Append('[');
                foreach (var c in p.Cards) sb.Append(c.GetInstanceID()).Append(';');
                sb.Append("]{");
                foreach (var cat in p.Categories)
                {
                    sb.Append(cat.Name).Append(':');
                    foreach (var st in cat.Stats) sb.Append(st.Name).Append(';');
                    sb.Append('/');
                }
                sb.Append("}\n");
            }
            return sb.ToString();
        }
    }
}

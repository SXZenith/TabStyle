using System.Collections.Generic;
using UnityEngine;

namespace TabStyle
{
    public enum LayoutKind { Split = 0, Compare = 1, Scoreboard = 2, Dock = 3, Classic = 4, Original = 5 }

    // Numbers are what's saved in the config, so they never change. Display order is Themes.Palettes.
    public enum PaletteKind { Midnight = 0, Ember = 1, Neon = 2, Arctic = 3, Crimson = 4, Mono = 5 }

    internal sealed class Palette
    {
        public PaletteKind Kind;
        public string Name;
        public Color Bg, Surf, Surf2, Line, Text, Muted, Accent, AccText;

        public Palette(PaletteKind kind, string bg, string surf, string surf2, string line, string text, string muted, string accent, string accText)
        {
            Kind = kind;
            Name = kind.ToString();
            Bg = Hex(bg); Surf = Hex(surf); Surf2 = Hex(surf2); Line = Hex(line);
            Text = Hex(text); Muted = Hex(muted); Accent = Hex(accent); AccText = Hex(accText);
        }

        internal static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }

    internal static class Themes
    {
        internal static readonly Palette[] Palettes =
        {
            new Palette(PaletteKind.Midnight, "#0F131B", "#171C26", "#202736", "#2B3344", "#E8ECF4", "#8F99AB", "#7FA2FF", "#0F131B"),
            new Palette(PaletteKind.Ember,    "#15100D", "#1F1813", "#2A2019", "#3A2D23", "#F5EBE3", "#AE9C90", "#FF8F45", "#15100D"),
            new Palette(PaletteKind.Neon,     "#0D0A18", "#161127", "#201936", "#30264D", "#EFEAFF", "#A398C9", "#3BE8D5", "#0D0A18"),
            new Palette(PaletteKind.Crimson,  "#0D0808", "#170E0F", "#231416", "#3B1D20", "#F4E7E8", "#B5999B", "#E63946", "#0D0808"),
            new Palette(PaletteKind.Mono,     "#0B0B0B", "#151515", "#202020", "#2E2E2E", "#F2F2F2", "#969696", "#FFFFFF", "#0B0B0B"),
            new Palette(PaletteKind.Arctic,   "#F3F5F9", "#FFFFFF", "#E6ECF6", "#D6DCE6", "#131A26", "#566173", "#2459D6", "#FFFFFF"),
        };

        internal static Palette Get(PaletteKind kind)
        {
            foreach (var p in Palettes) if (p.Kind == kind) return p;
            return Palettes[0];
        }

        internal static readonly LayoutKind[] Layouts = { LayoutKind.Split, LayoutKind.Compare, LayoutKind.Scoreboard, LayoutKind.Dock, LayoutKind.Classic, LayoutKind.Original };

        internal static string LayoutLabel(LayoutKind k)
        {
            switch (k)
            {
                case LayoutKind.Split: return "Split";
                case LayoutKind.Compare: return "Compare";
                case LayoutKind.Scoreboard: return "Scoreboard";
                case LayoutKind.Dock: return "Dock";
                case LayoutKind.Classic: return "Classic";
                case LayoutKind.Original: return "Original TabInfo";
                default: return k.ToString();
            }
        }

        // Health bar colors (same in every palette so low HP always reads as danger).
        internal static readonly Color HpGood = Palette.Hex("#3FBF7F");
        internal static readonly Color HpMid = Palette.Hex("#E5A93A");
        internal static readonly Color HpLow = Palette.Hex("#E5484D");

        internal static Color HpColor(float pct)
        {
            return pct > 0.5f ? HpGood : pct > 0.25f ? HpMid : HpLow;
        }

        // Stats promoted to columns / tiles. Name is TabInfo's (or CatInfo's) stat name; Short is the label.
        internal static readonly KeyValuePair<string, string>[] QuickStats =
        {
            new KeyValuePair<string, string>("Damage", "DMG"),
            new KeyValuePair<string, string>("Attack Speed", "ATK SPD"),
            new KeyValuePair<string, string>("Block Cooldown", "BLOCK"),
            new KeyValuePair<string, string>("Reload Time", "RELOAD"),
            new KeyValuePair<string, string>("Ammo", "AMMO"),
            new KeyValuePair<string, string>("Movespeed", "MOVE"),
        };

        internal static readonly KeyValuePair<string, string>[] DockExtraStats =
        {
            new KeyValuePair<string, string>("Bullets", "BULLETS"),
            new KeyValuePair<string, string>("Bounces", "BOUNCES"),
        };
    }
}

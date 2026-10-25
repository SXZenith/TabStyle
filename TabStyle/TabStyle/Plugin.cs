using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace TabStyle
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(TabInfoGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class TabStyle : BaseUnityPlugin
    {
        public const string ModId = "com.zen.rounds.tabstyle";
        public const string ModName = "TabStyle";
        public const string Version = "1.0.0";
        public const string ModInitials = "Zen";
        internal const string TabInfoGuid = "com.willuwontu.rounds.tabinfo";

        internal static TabStyle Instance { get; private set; }
        internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

        internal static ConfigEntry<LayoutKind> LayoutSetting;
        internal static ConfigEntry<PaletteKind> PaletteSetting;
        internal static ConfigEntry<bool> ShowCardsSetting;
        internal static ConfigEntry<bool> YouFirstSetting;
        internal static ConfigEntry<string> CollapsedSetting;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            LayoutSetting = Config.Bind("Theme", "Layout", LayoutKind.Split,
                "Which layout the Tab screen uses. You can also change this with the buttons in the top right of the Tab screen.");
            PaletteSetting = Config.Bind("Theme", "Colors", PaletteKind.Midnight,
                "Which color palette the Tab screen uses. You can also change this with the color dots in the top right of the Tab screen.");
            ShowCardsSetting = Config.Bind("Theme", "ShowCards", false,
                "Show each player's cards. Toggle it with the CARDS button in the top right of the Tab screen.");
            YouFirstSetting = Config.Bind("Theme", "YouFirst", true,
                "Put you at the top (or left) of every list so you never have to scroll to find yourself. Toggle it with the YOU FIRST button in the top right of the Tab screen.");
            CollapsedSetting = Config.Bind("Theme", "CollapsedSections", "",
                "Stat sections you collapsed (remembered between games). Managed by the mod.");

            new Harmony(ModId).PatchAll(typeof(TabStyle).Assembly);
        }

        private void Start()
        {
            Unbound.RegisterClientSideMod(ModId);
            Unbound.RegisterCredits(ModName,
                new[] { "Zen", "Built on TabInfo by willuwontu" },
                new[] { "Thunderstore" },
                new[] { "https://thunderstore.io/c/rounds/p/Zenith/" });

            gameObject.AddComponent<TabStyleUI>();

            GameModeManager.AddHook(GameModeHooks.HookGameStart, CloseHook);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, CloseHook);
            Unbound.OnLeftRoom += () => TabStyleUI.Instance?.Close();
        }

        private static IEnumerator CloseHook(IGameModeHandler gm)
        {
            TabStyleUI.Instance?.Close();
            yield break;
        }
    }
}

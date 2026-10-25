using HarmonyLib;
using TabInfo.Utils;

namespace TabStyle
{
    internal static class Mode
    {
        /// <summary>True when TabStyle's own window should be used (not the "Original TabInfo" theme, and TabStyle hooked in fine).</summary>
        internal static bool Styled => TabStyleUI.Ready && TabStyle.LayoutSetting.Value != LayoutKind.Original;
    }

    /// <summary>When Tab is pressed, open TabStyle instead of TabInfo's window.
    /// With the "Original TabInfo" theme (or if TabStyle couldn't hook in), TabInfo's own window opens.</summary>
    [HarmonyPatch(typeof(TabInfoManager), nameof(TabInfoManager.ToggleTabFrame))]
    internal static class ToggleTabFramePatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix()
        {
            if (!Mode.Styled) return true;
            TabStyleUI.Instance.Toggle();
            return false;
        }

        // Original theme: give TabInfo's window the little theme buttons so you can switch back.
        private static void Postfix()
        {
            if (TabStyleUI.Ready && TabStyle.LayoutSetting.Value == LayoutKind.Original)
                OriginalMode.Attach();
        }
    }

    /// <summary>Other mods ask TabInfo "is the tab window open?" - answer for TabStyle's window.</summary>
    [HarmonyPatch(typeof(TabInfoManager), nameof(TabInfoManager.IsLockingInput), MethodType.Getter)]
    internal static class IsLockingInputPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ref bool __result)
        {
            if (!Mode.Styled) return true;
            // Also "true" for the frame in which Esc just closed TabStyle, so TabInfo's own Esc
            // check agrees with ours and the pause menu doesn't pop open behind it.
            __result = TabStyleUI.Instance.IsOpen || TabStyleUI.Instance.ClosedByEscThisFrame;
            return false;
        }
    }

    /// <summary>Esc closes the Tab screen first (instead of opening the pause menu on top of it).</summary>
    [HarmonyPatch(typeof(EscapeMenuHandler), "ToggleEsc")]
    internal static class EscapeMenuPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix()
        {
            if (TabStyleUI.Ready && TabStyleUI.Instance.IsOpen)
            {
                TabStyleUI.Instance.CloseFromEsc();
                return false;
            }
            return true;
        }
    }
}

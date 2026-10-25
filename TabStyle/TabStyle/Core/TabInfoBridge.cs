using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TabInfo.Utils;
using UnboundLib.GameModes;
using UnityEngine;

namespace TabStyle
{
    /// <summary>
    /// Everything TabStyle needs from TabInfo. TabInfo keeps a few members internal, so those are
    /// reached through reflection once at startup. Every call is guarded: a broken stat from some
    /// other mod can never take the UI down with it.
    /// </summary>
    internal static class TabInfoBridge
    {
        private static Func<Stat, Player, bool> statVisible;
        private static Func<Stat, Player, string> statValue;
        private static Func<StatCategory, Player, bool> categoryVisible;
        private static FieldInfo canvasField, cardHolderField, statObjectField, hiddenModesField;
        private static bool initialized;

        internal static bool Ready { get; private set; }

        internal static void Init()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                statVisible = MakeDelegate<Func<Stat, Player, bool>>(typeof(Stat), "DisplayCondition");
                statValue = MakeDelegate<Func<Stat, Player, string>>(typeof(Stat), "DisplayValue");
                categoryVisible = MakeDelegate<Func<StatCategory, Player, bool>>(typeof(StatCategory), "DisplayCondition");

                canvasField = AccessTools.Field(typeof(TabInfoManager), "canvas");
                cardHolderField = AccessTools.Field(typeof(TabInfoManager), "cardHolderTemplate");
                statObjectField = AccessTools.Field(typeof(TabInfoManager), "statObjectTemplate");
                hiddenModesField = AccessTools.Field(typeof(TabInfoManager), "hiddenGameModes");

                Ready = statVisible != null && statValue != null && categoryVisible != null;
                if (!Ready) TabStyle.Log.LogError("Could not hook TabInfo's stat methods. TabStyle will leave TabInfo's own UI in place.");
            }
            catch (Exception e)
            {
                Ready = false;
                TabStyle.Log.LogError("Failed to read TabInfo. TabStyle will leave TabInfo's own UI in place.\n" + e);
            }
        }

        private static T MakeDelegate<T>(Type type, string name) where T : class
        {
            var method = AccessTools.Method(type, name, new[] { typeof(Player) });
            if (method == null) return null;
            try
            {
                return Delegate.CreateDelegate(typeof(T), method) as T;
            }
            catch
            {
                // Fallback: slower reflection call with the same shape.
                if (typeof(T) == typeof(Func<Stat, Player, bool>))
                    return (T)(object)new Func<Stat, Player, bool>((s, p) => (bool)method.Invoke(s, new object[] { p }));
                if (typeof(T) == typeof(Func<Stat, Player, string>))
                    return (T)(object)new Func<Stat, Player, string>((s, p) => (string)method.Invoke(s, new object[] { p }));
                if (typeof(T) == typeof(Func<StatCategory, Player, bool>))
                    return (T)(object)new Func<StatCategory, Player, bool>((c, p) => (bool)method.Invoke(c, new object[] { p }));
                return null;
            }
        }

        internal static GameObject Canvas => Get<GameObject>(canvasField);
        internal static GameObject CardHolderTemplate => Get<GameObject>(cardHolderField);

        private static T Get<T>(FieldInfo f) where T : class
        {
            try { return f?.GetValue(null) as T; } catch { return null; }
        }

        internal static TMPro.TMP_FontAsset FindFont()
        {
            try
            {
                var template = Get<GameObject>(statObjectField);
                var tmp = template != null ? template.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) : null;
                if (tmp != null && tmp.font != null) return tmp.font;
            }
            catch { }
            if (TMPro.TMP_Settings.defaultFontAsset != null) return TMPro.TMP_Settings.defaultFontAsset;
            return Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>().FirstOrDefault();
        }

        internal static bool IsHiddenGameMode()
        {
            try
            {
                var list = Get<List<string>>(hiddenModesField);
                var id = GameModeManager.CurrentHandlerID;
                return list != null && id != null && list.Contains(id);
            }
            catch { return false; }
        }

        internal static bool StatVisible(Stat s, Player p)
        {
            try { return statVisible(s, p); } catch { return false; }
        }

        internal static string StatValue(Stat s, Player p)
        {
            try { return statValue(s, p) ?? ""; } catch { return ""; }
        }

        internal static bool CategoryVisible(StatCategory c, Player p)
        {
            try { return categoryVisible(c, p); } catch { return false; }
        }

        internal static string BasicCategoryName
        {
            get { try { return TabInfoManager.basicStats?.name ?? "Basic Stats"; } catch { return "Basic Stats"; } }
        }

        /// <summary>Categories in TabInfo's order (priority, then name).</summary>
        internal static List<StatCategory> OrderedCategories()
        {
            try
            {
                return TabInfoManager.Categories.Values
                    .Where(c => c != null)
                    .OrderBy(c => c.priority)
                    .ThenBy(c => (c.name ?? "").ToLowerInvariant())
                    .ToList();
            }
            catch { return new List<StatCategory>(); }
        }

        internal static List<Stat> OrderedStats(StatCategory c)
        {
            try
            {
                return c.Stats.Values.Where(s => s != null).OrderBy(s => s.name ?? "").ToList();
            }
            catch { return new List<Stat>(); }
        }
    }
}

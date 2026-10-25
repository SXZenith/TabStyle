using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>
    /// Owns the TabStyle window: opening/closing, reading stats a few times a second,
    /// rebuilding only when the shape of the data changes, and updating numbers in place otherwise.
    /// </summary>
    internal partial class TabStyleUI : MonoBehaviour
    {
        internal static TabStyleUI Instance { get; private set; }

        private const float RefreshInterval = 0.2f;
        private const float DesignW = 1920f, DesignH = 1080f;

        internal GameObject CanvasObject { get; private set; }
        internal bool IsOpen { get; private set; }

        private GameObject ownCanvas;
        private RectTransform root, frame, content;
        private CanvasGroup frameGroup;
        private AudioSource audioSource;
        private static readonly System.Random rng = new System.Random();

        private Snapshot snap;
        private Palette pal;
        private float nextRefresh;
        private bool dirty;
        private string builtKey;
        private Vector2 designSize = new Vector2(DesignW, DesignH);
        private float openTime;

        // Things the refresh updates in place.
        private readonly Dictionary<string, List<TextMeshProUGUI>> texts = new Dictionary<string, List<TextMeshProUGUI>>();
        private readonly List<HpBind> hpBinds = new List<HpBind>();
        private readonly Dictionary<string, SmartScroll> scrolls = new Dictionary<string, SmartScroll>();
        private readonly Dictionary<string, Vector2> scrollMemory = new Dictionary<string, Vector2>();

        // View state (kept while the game runs).
        private int selectedId = int.MinValue;
        private readonly HashSet<int> expanded = new HashSet<int>();
        private bool expandedInitialized;
        private int dockOpenId = int.MinValue;
        private bool dockAllStats;
        private readonly HashSet<int> classicCollapsed = new HashSet<int>();
        private readonly HashSet<string> collapsed = new HashSet<string>();

        private TextMeshProUGUI hint;
        private string hintDefault = "";

        private sealed class HpBind
        {
            public int Id;
            public RectTransform Fill;
            public Image FillImage;
            public TextMeshProUGUI Text;
            public float Current, Target;
        }

        // ------------------------------------------------------------------ lifecycle
        private void Awake()
        {
            Instance = this;
            TabInfoBridge.Init();
            foreach (var s in (TabStyle.CollapsedSetting.Value ?? "").Split('|'))
                if (!string.IsNullOrEmpty(s)) collapsed.Add(s);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        internal static bool Ready => Instance != null && TabInfoBridge.Ready;

        internal void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        internal void Open()
        {
            if (!TabInfoBridge.Ready || IsOpen) return;
            if (TabStyle.LayoutSetting.Value == LayoutKind.Original) return;
            if (!GameIsRunning() || TabInfoBridge.IsHiddenGameMode()) return;
            if (!EnsureCanvas()) return;
            EnsureEventSystem();

            if (UIKit.Font == null) UIKit.Font = TabInfoBridge.FindFont();

            snap = Snapshot.Take(TabStyle.YouFirstSetting.Value);
            if (snap.Players.Count == 0) return;

            IsOpen = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            openTime = Time.unscaledTime;
            frameGroup.alpha = 0f;
            UpdateScale();
            Rebuild();
            nextRefresh = Time.unscaledTime + RefreshInterval;
        }

        internal void Close()
        {
            CardPreview.Hide();
            if (!IsOpen) return;
            IsOpen = false;
            if (root != null)
            {
                RememberScrolls();
                ClearContent();
                root.gameObject.SetActive(false);
            }
        }

        private int escCloseFrame = -1;
        internal bool ClosedByEscThisFrame => escCloseFrame == Time.frameCount;

        internal void CloseFromEsc()
        {
            escCloseFrame = Time.frameCount;
            Close();
        }

        private static bool GameIsRunning()
        {
            try
            {
                var pm = PlayerManager.instance;
                return pm != null && pm.players != null && pm.players.Count > 0 && GameModeManager.CurrentHandler != null;
            }
            catch { return false; }
        }

        private void Update()
        {
            if (!IsOpen) return;

            if (root == null || CanvasObject == null || !GameIsRunning() || TabInfoBridge.IsHiddenGameMode())
            {
                Close();
                return;
            }

            UpdateScale();

            // Open animation: quick fade + settle.
            float t = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.14f);
            float ease = 1f - (1f - t) * (1f - t);
            frameGroup.alpha = ease;

            if (dirty || Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshInterval;
                try { Refresh(); }
                catch (Exception e) { TabStyle.Log.LogError("Refresh failed: " + e); }
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < hpBinds.Count; i++)
            {
                var b = hpBinds[i];
                if (b.Fill == null) continue;
                if (!Mathf.Approximately(b.Current, b.Target))
                {
                    b.Current = Mathf.MoveTowards(b.Current, b.Target, Mathf.Max(0.02f, Mathf.Abs(b.Target - b.Current)) * dt * 10f);
                    b.Fill.anchorMax = new Vector2(b.Current, 1f);
                    b.FillImage.color = Themes.HpColor(b.Current);
                }
            }
        }

        // ------------------------------------------------------------------ canvas
        private bool EnsureCanvas()
        {
            GameObject canvas = TabInfoBridge.Canvas;
            if (canvas == null)
            {
                if (ownCanvas == null)
                {
                    ownCanvas = new GameObject("TabStyle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var c = ownCanvas.GetComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.sortingOrder = 30000;
                    var sc = ownCanvas.GetComponent<CanvasScaler>();
                    sc.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    DontDestroyOnLoad(ownCanvas);
                }
                canvas = ownCanvas;
            }

            if (CanvasObject != canvas || root == null)
            {
                if (root != null) Destroy(root.gameObject);
                CanvasObject = canvas;
                BuildRoot();
            }
            return root != null;
        }

        private void BuildRoot()
        {
            root = UIKit.Rect(CanvasObject.transform, "TabStyle");
            UIKit.Stretch(root);
            audioSource = root.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            frame = UIKit.Rect(root, "Frame");
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(DesignW, DesignH);
            frameGroup = frame.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("TabStyle EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        /// <summary>Lay out in a 1920x1080-ish design space and scale to the real screen, whatever TabInfo's canvas does.</summary>
        private void UpdateScale()
        {
            if (root == null || frame == null) return;
            var size = root.rect.size;
            if (size.x < 2f || size.y < 2f) return;
            float s = size.x / size.y >= DesignW / DesignH ? size.y / DesignH : size.x / DesignW;
            if (s <= 0.0001f) return;
            var newDesign = new Vector2(Mathf.Round(size.x / s), Mathf.Round(size.y / s));
            frame.localScale = new Vector3(s, s, 1f);
            frame.sizeDelta = newDesign;
            if ((newDesign - designSize).sqrMagnitude > 4f)
            {
                designSize = newDesign;
                dirty = true;
            }
        }

        // ------------------------------------------------------------------ refresh / rebuild
        private string ViewKey()
        {
            return string.Concat(
                TabStyle.LayoutSetting.Value, "|", TabStyle.PaletteSetting.Value, "|", TabStyle.ShowCardsSetting.Value, "|", TabStyle.YouFirstSetting.Value, "|",
                selectedId, "|", string.Join(",", expanded.OrderBy(x => x)), "|", dockOpenId, "|", dockAllStats, "|", string.Join(",", classicCollapsed.OrderBy(x => x)), "|",
                string.Join(",", collapsed.OrderBy(x => x)), "|", designSize.x, "x", designSize.y, "|", snap.StructureKey);
        }

        private void Refresh()
        {
            snap = Snapshot.Take(TabStyle.YouFirstSetting.Value);
            if (snap.Players.Count == 0) { Close(); return; }
            FixState();
            if (dirty || ViewKey() != builtKey) Rebuild();
            else UpdateValues();
        }

        private void FixState()
        {
            if (snap.Find(selectedId) == null) selectedId = snap.FirstLocal.Id;
            if (!expandedInitialized)
            {
                expandedInitialized = true;
                foreach (var p in snap.Players.Where(p => p.Local)) expanded.Add(p.Id);
            }
            expanded.RemoveWhere(id => snap.Find(id) == null);
            if (dockOpenId != int.MinValue && snap.Find(dockOpenId) == null) dockOpenId = int.MinValue;
            classicCollapsed.RemoveWhere(id => snap.Find(id) == null);
        }

        private void RememberScrolls()
        {
            foreach (var kv in scrolls)
                if (kv.Value != null && kv.Value.content != null)
                    scrollMemory[kv.Key] = kv.Value.content.anchoredPosition;
        }

        private void ClearContent()
        {
            CardPreview.Hide();
            texts.Clear();
            hpBinds.Clear();
            scrolls.Clear();
            hint = null;
            if (content != null)
            {
                content.gameObject.SetActive(false);
                Destroy(content.gameObject);
                content = null;
            }
        }

        private void Rebuild()
        {
            dirty = false;
            FixState();
            RememberScrolls();
            ClearContent();

            pal = Themes.Get(TabStyle.PaletteSetting.Value);
            content = UIKit.Rect(frame, "Content");
            UIKit.Stretch(content);

            try
            {
                switch (TabStyle.LayoutSetting.Value)
                {
                    case LayoutKind.Compare: BuildCompare(); break;
                    case LayoutKind.Scoreboard: BuildScoreboard(); break;
                    case LayoutKind.Dock: BuildDock(); break;
                    case LayoutKind.Classic: BuildClassic(); break;
                    default: BuildSplit(); break;
                }
            }
            catch (Exception e)
            {
                TabStyle.Log.LogError("Building the Tab screen failed: " + e);
            }

            builtKey = ViewKey();

            // Lay everything out now so scroll positions can be restored without a visible jump.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            foreach (var kv in scrolls)
            {
                var sr = kv.Value;
                if (sr == null || !scrollMemory.TryGetValue(kv.Key, out var pos)) continue;
                var view = sr.viewport.rect.size;
                var size = sr.content.rect.size;
                pos.x = Mathf.Clamp(pos.x, -Mathf.Max(0f, size.x - view.x), 0f);
                pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, size.y - view.y));
                sr.content.anchoredPosition = pos;
            }
        }

        private void UpdateValues()
        {
            SetText("title", TitleText());
            SetText("subtitle", SubtitleText());
            foreach (var team in snap.Teams)
                SetText("tscore|" + team.Team, ScoreText(team.Rounds, team.Points, false));
            foreach (var p in snap.Players)
            {
                SetText("pscore|" + p.Id, ScoreText(p.Rounds, p.Points, true));
                SetText("pteam|" + p.Id, TeamLine(p));
                foreach (var cat in p.Categories)
                    foreach (var st in cat.Stats)
                        SetText(StatKey(p.Id, cat.Name, st.Name), st.Value);
            }
            foreach (var b in hpBinds)
            {
                var p = snap.Find(b.Id);
                if (p == null) continue;
                b.Target = p.HpPct;
                if (b.Text != null) SetTmp(b.Text, HpText(p));
            }
        }

        // ------------------------------------------------------------------ binding helpers
        private static string StatKey(int id, string cat, string stat) => id + "|" + cat + "|" + stat;

        private void Bind(string key, TextMeshProUGUI tmp)
        {
            if (!texts.TryGetValue(key, out var list)) texts[key] = list = new List<TextMeshProUGUI>();
            list.Add(tmp);
        }

        private void SetText(string key, string value)
        {
            if (!texts.TryGetValue(key, out var list)) return;
            foreach (var t in list) SetTmp(t, value);
        }

        private static void SetTmp(TextMeshProUGUI t, string value)
        {
            if (t != null && t.text != value) t.text = value ?? "";
        }

        private string TitleText()
        {
            var n = string.IsNullOrEmpty(snap.ModeName) ? "Game" : snap.ModeName;
            return n.ToUpperInvariant();
        }

        private string SubtitleText()
        {
            var s = "Round " + snap.Round + "     Point " + snap.Point + "     " + snap.Players.Count + (snap.Players.Count == 1 ? " player" : " players");
            if (snap.RoundsToWin > 0) s += "     First to " + snap.RoundsToWin + " rounds";
            return s;
        }

        private string ScoreText(int rounds, int points, bool compact)
        {
            if (compact) return rounds + "R  " + points + "P";
            return rounds + "/" + snap.RoundsToWin + " Rounds    " + points + "/" + snap.PointsToWin + " Points";
        }

        private string TeamLine(PlayerInfo p)
        {
            return p.TeamName + "     " + ScoreText(p.Rounds, p.Points, false);
        }

        private static string HpText(PlayerInfo p)
        {
            return string.Format("{0:F0} / {1:F0}", Mathf.Max(0f, p.Hp), Mathf.Max(0f, p.MaxHp));
        }

        internal static void PlayClick()
        {
            try
            {
                var inst = Instance;
                var clips = global::TabInfo.TabInfo.instance != null ? global::TabInfo.TabInfo.instance.click : null;
                if (inst == null || inst.audioSource == null || clips == null || clips.Count == 0) return;
                var clip = clips[rng.Next(clips.Count)];
                if (clip != null) inst.audioSource.PlayOneShot(clip, 0.8f);
            }
            catch { }
        }

        // ------------------------------------------------------------------ state changes (always applied on the next frame)
        private void SetLayout(LayoutKind k)
        {
            if (TabStyle.LayoutSetting.Value == k) return;
            TabStyle.LayoutSetting.Value = k;
            if (k == LayoutKind.Original)
            {
                // Hand over to TabInfo's own window (our Tab patch now lets TabInfo run).
                Close();
                OriginalMode.OpenTabInfo();
                return;
            }
            dirty = true;
        }

        /// <summary>Called from the switcher on TabInfo's original window.</summary>
        internal void SwitchFromOriginal(LayoutKind k)
        {
            TabStyle.LayoutSetting.Value = k;
            OriginalMode.CloseTabInfo();
            Open();
        }
        private void SetPalette(PaletteKind k) { if (TabStyle.PaletteSetting.Value != k) { TabStyle.PaletteSetting.Value = k; dirty = true; } }
        private void ToggleCards() { TabStyle.ShowCardsSetting.Value = !TabStyle.ShowCardsSetting.Value; dirty = true; }
        private void ToggleYouFirst() { TabStyle.YouFirstSetting.Value = !TabStyle.YouFirstSetting.Value; dirty = true; }

        private void ToggleCollapsed(string category)
        {
            if (!collapsed.Remove(category)) collapsed.Add(category);
            TabStyle.CollapsedSetting.Value = string.Join("|", collapsed.OrderBy(x => x));
            dirty = true;
        }

        private void Select(int id) { selectedId = id; dirty = true; }
        private void ToggleExpanded(int id) { if (!expanded.Remove(id)) expanded.Add(id); dirty = true; }
        private void ToggleDockOpen(int id) { dockOpenId = dockOpenId == id ? int.MinValue : id; dirty = true; }
        private void ToggleDockAllStats() { dockAllStats = !dockAllStats; dirty = true; }
        private void ToggleClassic(int id) { if (!classicCollapsed.Remove(id)) classicCollapsed.Add(id); dirty = true; }
    }
}

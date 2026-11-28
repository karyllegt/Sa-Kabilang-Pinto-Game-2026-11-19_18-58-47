using System.Collections.Generic;
using Skp.Core;
using Skp.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Skp.UI
{
    /// <summary>
    /// The Map scene: draws the overworld (blended act backdrop, dotted path, nine nodes with numbers
    /// above them, Lola with her "!" marker, the Tala token), walks the token, follows it with the camera
    /// and drives the <see cref="MapScreen"/> overlay and Lola's dialog. Arrows/D-pad step between nodes,
    /// Submit or a tap on the selected node enters it, Up reaches the top bar, Back returns to the title.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [Header("Scene")]
        public MapScreen screen;
        public DialogBox dialog;
        public Camera cam;
        public SpriteRenderer background;
        [Header("Art")]
        [Tooltip("R1: the map's dots, node plates and icons, numbers, corners, Lola, her marker and the token. Falls back to Resources.")]
        public Skp.Levels.LevelPrefabCatalog catalog;
        public Sprite white, star, bell, lola;
        public SkillsScreen skillsScreen;
        public Sprite[] boyFrames = new Sprite[4], girlFrames = new Sprite[4];
        public TMP_FontAsset font;
        internal UiTheme lanternTheme;
        internal SpriteRenderer lanternLabelBacking;
        [Tooltip("Level names come from the level data assets.")]
        public Skp.Levels.LevelConfig[] levels = new Skp.Levels.LevelConfig[9];

        public MapModel Model { get; private set; }
        public float CameraX { get; private set; }
        /// <summary>How many times Lola has spoken in this scene (tests: the first meeting shows exactly once).</summary>
        public int LolaTalks { get; private set; }
        public bool Entering { get; private set; }

        static readonly string[] DiffCycle = { "story", "standard", "nightmare" };
        const float FirstMeetingDelay = 0.7f;

        readonly List<Sprite> _landmarks = new List<Sprite>();
        readonly List<SpriteRenderer> _nodeIcons = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _nodePlates = new List<SpriteRenderer>();
        readonly List<TextMeshPro> _nodeNumbers = new List<TextMeshPro>();
        readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
        readonly List<int> _dotSegment = new List<int>();
        readonly List<Transform> _corners = new List<Transform>();
        public Sprite[] lolaFrames = new Sprite[0];
        SpriteRenderer _token, _lola;
        GameObject _lolaMark;
        Transform _world;
        float _t, _firstMeetingAt = -1;
        SaveData _save;
        string _diff;

        GameRoot Root => GameRoot.Instance;

        void Start()
        {
            Skp.Audio.Music.SetDimension(Skp.Dimensions.Dim.Ordinary);
            Skp.Audio.Music.Play(Skp.Audio.MusicTrack.Menu);
            var save = Root != null ? Root.Save.Data : new SaveData();
            int from = GameSession.MapFrom;
            GameSession.MapFrom = -1;
            Present(save, from);
            Wire();
            screen.Open();
            if (!save.metLola) _firstMeetingAt = Time.unscaledTime + FirstMeetingDelay;
        }

        void Wire()
        {
            screen.BackRequested = () => { if (!Entering) Root.Loader.Load(SceneNames.Title); };
            screen.back.Clicked += () => screen.OnBack();
            screen.talk.Clicked += TalkToLola;
            screen.enter.Clicked += () => Enter();
            screen.enter.ClickedWhileDisabled += () => { };
            screen.difficulty.Clicked += CycleDifficulty;
            screen.skills.Clicked += ShowSkills;
            screen.tapArea.Tapped += OnTap;
            UiNavigator.Fallback = () => true;
        }

        void OnDestroy() => UiNavigator.Fallback = null;

        /// <summary>Builds the map for a save (also used by the screenshot audit in edit mode).</summary>
        public void Present(SaveData save, int from = -1)
        {
            _save = save;
            _diff = System.Array.IndexOf(DiffCycle, save.diff) >= 0 ? save.diff : "standard";
            Model = new MapModel(save, from);
            BuildWorld();
            CameraX = TargetCameraX();
            Refresh();
        }

        // ------------------------------------------------------------------ world

        Skp.Levels.LevelPrefabCatalog Catalog => catalog != null ? catalog : (catalog = Skp.Levels.LevelPrefabCatalog.Load());

        void BuildWorld()
        {
            if (_world != null) DestroyImmediate(_world.gameObject);
            _world = new GameObject("MapWorld").transform; // an empty folder; everything drawn below is a prefab instance
            _world.SetParent(transform, false);
            _landmarks.Clear();
            _nodeIcons.Clear(); _nodePlates.Clear(); _nodeNumbers.Clear(); _dots.Clear(); _dotSegment.Clear(); _corners.Clear();
            var cat = Catalog;

            // dotted path between nodes, every 9 px
            for (int i = 0; i < MapModel.Nodes.Length - 1; i++)
            {
                Vector2 a = MapModel.Nodes[i], b = MapModel.Nodes[i + 1];
                int n = Mathf.FloorToInt(Vector2.Distance(a, b) / 9f);
                for (int k = 1; k < n; k++)
                {
                    var p = Vector2.Lerp(a, b, k / (float)n);
                    _dots.Add(Place(cat.mapDot, p + new Vector2(1.5f, 1)).main);
                    if (lanternTheme != null) _dots[_dots.Count - 1].transform.localScale *= 1.55f;
                    _dotSegment.Add(i);
                }
            }
            for (int i = 0; i < MapModel.Nodes.Length; i++)
            {
                var p = MapModel.Nodes[i];
                Place(cat.mapNodeShadow, p + new Vector2(0, 3.5f));
                _nodePlates.Add(Place(cat.mapNodePlate, p + new Vector2(0, 1.5f)).main);
                var icon = Place(cat.mapNodeIcon, p + new Vector2(0, i == 8 ? -10 : -11));
                icon.SetState(i == 8 ? 1 : 0); // flame star, the last node's bell
                _nodeIcons.Add(icon.main);
                _landmarks.Add(icon.main.sprite);
                if (lanternTheme != null)
                {
                    icon.main.transform.localScale *= 1.5f;
                    _nodePlates[i].transform.localScale *= 1.18f;
                }
                // the number sits ABOVE the node so the info panel never hides it (docs/04 issue 3)
                var num = Place(cat.mapNumber, p + new Vector2(0, lanternTheme != null ? -29 : -27)).label;
                num.text = (i + 1).ToString();
                if (lanternTheme != null) BackMapLabel(num);
                _nodeNumbers.Add(num);
            }
            for (int c = 0; c < 4; c++) _corners.Add(Skp.Levels.LevelPiece.Spawn(cat.mapCorner, _world, _world.position).transform);

            _lola = Place(cat.mapLola, MapModel.Lola + new Vector2(7, 20)).main;
            var mark = Skp.Levels.LevelPiece.Spawn(cat.mapLolaMark, _world, _world.position);
            Vector3 at = MapModel.ToWorld(MapModel.Lola + new Vector2(7.5f, -8.5f));
            mark.main.transform.position = at;
            mark.label.transform.position = at;
            _lolaMark = mark.gameObject;
            if (lanternTheme != null)
            {
                BackMapLabel(mark.label);
                mark.main.transform.localScale *= 1.2f;
            }

            _token = Place(cat.mapToken, MapModel.Nodes[0]).main;
            _token.sprite = Frames()[0];
            if (lanternTheme != null) _token.transform.localScale *= 1.16f;
        }

        void BackMapLabel(TextMeshPro label)
        {
            label.fontSize = 5.5f;
            label.color = lanternTheme.text;
            if (lanternLabelBacking == null) return;
            label.ForceMeshUpdate(true);
            var ink = label.textBounds;
            var backing = Instantiate(lanternLabelBacking, label.transform, false);
            backing.transform.localPosition = ink.center;
            backing.color = lanternTheme.panel;
            backing.size = new Vector2(ink.size.x + 0.24f, ink.size.y + 0.2f);
            backing.sortingLayerID = label.renderer.sortingLayerID;
            backing.sortingOrder = 9;
            label.renderer.sortingOrder = 10;
        }

        Sprite[] Frames() => _save != null && _save.@char == "boy" ? boyFrames : girlFrames;

        /// <summary>A map prefab placed at a map position (pixels).</summary>
        Skp.Levels.LevelPiece Place(Skp.Levels.LevelPiece prefab, Vector2 px) => Skp.Levels.LevelPiece.Spawn(prefab, _world, MapModel.ToWorld(px));

        // ------------------------------------------------------------------ state

        void Refresh()
        {
            var th = UiKit.Theme;
            for (int i = 0; i < MapModel.Nodes.Length; i++)
            {
                var st = Model.State(i);
                _nodePlates[i].color = st == NodeState.Cleared ? th.gold : st == NodeState.Ready ? new Color32(0xc2, 0x8a, 0x4a, 0xff) : new Color32(0x3a, 0x33, 0x40, 0xff);
                _nodeIcons[i].color = st == NodeState.Locked ? new Color(0.45f, 0.42f, 0.5f, 0.5f) : Color.white;
                _nodeNumbers[i].color = st == NodeState.Locked ? (Color)new Color32(0x9a, 0x90, 0xa8, 0xff) : th.text;
                if (lanternTheme == null) continue;
                var icon = _nodeIcons[i];
                bool current = i == Model.Selected;
                _nodePlates[i].color = current ? th.text : st == NodeState.Cleared ? th.teal :
                    st == NodeState.Ready ? th.goldBright : th.textDisabled;
                icon.sprite = st == NodeState.Locked ? th.Mark(LanternMark.Locked) : current ?
                    th.Mark(LanternMark.Parol) : st == NodeState.Cleared ?
                    th.Mark(LanternMark.Cleared) : _landmarks[i];
                icon.color = Color.white;
                _nodeNumbers[i].color = st == NodeState.Locked ? th.textDisabled : th.text;
            }
            for (int d = 0; d < _dots.Count; d++)
            {
                bool lit = _dotSegment[d] + 1 < _save.unlocked;
                _dots[d].color = lit ? th.gold : new Color32(0x4a, 0x3a, 0x4a, 0xff);
                if (lanternTheme != null)
                    _dots[d].color = lit ? th.goldBright : new Color(0.78f, 0.71f, 0.86f, 0.8f);
            }
            _lolaMark.SetActive(Model.LolaHasNews);

            int i0 = Model.InfoNode;
            var spec = LevelName(i0);
            var state = Model.State(i0);
            screen.levelName.text = Loc.Format("map.node", i0 + 1, spec.ToUpperInvariant());
            string stateText = state == NodeState.Cleared ? Loc.Get("map.cleared") : state == NodeState.Ready ? Loc.Get("map.ready") : Loc.Format("map.locked", i0);
            screen.levelSub.text = Loc.Format("map.sub", Skp.Levels.LevelCatalog.ActNames[MapModel.ActOf(i0)], stateText);
            screen.enter.Text = Loc.Get(state == NodeState.Cleared ? "map.replay" : "map.enter");
            screen.enter.Interactable = state != NodeState.Locked;
            screen.act.text = Skp.Levels.LevelCatalog.ActNames[MapModel.ActOf(Model.Selected)];
            screen.difficulty.Text = Loc.Get("diff." + _diff);
            PlaceToken();
        }

        string LevelName(int i)
        {
            if (levels != null && i < levels.Length && levels[i] != null) return levels[i].spec.name;
            return Skp.Levels.LevelCatalog.Defaults()[Mathf.Clamp(i, 0, 8)].name;
        }

        void PlaceToken()
        {
            _token.transform.position = MapModel.ToWorld(Model.Token);
            var f = Frames();
            _token.sprite = Model.Walking ? f[1 + ((int)(_t * 7.5f) & 1)] : f[0];
            _token.flipX = Model.Facing < 0;
            var sel = MapModel.Nodes[Model.Selected];
            int b = Mathf.Sin(_t * 9f) > 0 ? 1 : 0;
            float l = sel.x - 11 - b, r = sel.x + 11 + b, top = sel.y - 24 - b, bot = sel.y + 7 + b;
            SetCorner(0, l, top, 1, 1); SetCorner(1, r, top, -1, 1); SetCorner(2, l, bot, 1, -1); SetCorner(3, r, bot, -1, -1);
            if (cam != null)
            {
                float halfH = cam.orthographicSize;
                cam.transform.position = new Vector3(CameraX / 16f + HalfViewPx() / 16f, halfH, -10);
            }
        }

        void SetCorner(int k, float x, float y, int sx, int sy)
        {
            var c = _corners[k];
            c.GetChild(0).position = MapModel.ToWorld(new Vector2(x + sx * 2f, y + 0.5f * sy));
            c.GetChild(1).position = MapModel.ToWorld(new Vector2(x + 0.5f * sx, y + sy * 2f));
        }

        float HalfViewPx() => cam != null ? cam.orthographicSize * cam.aspect * 16f : MapModel.ViewWidth / 2f;

        float TargetCameraX() => Mathf.Clamp(Model.Token.x - HalfViewPx(), 0, Mathf.Max(0, MapModel.MapWidth - 2 * HalfViewPx()));

        // ------------------------------------------------------------------ input

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_lola != null && lolaFrames != null && lolaFrames.Length >= 4)
            {
                int frame = dialog != null && dialog.IsOpen ? 1 + ((int)(_t * 3) % 3) : ((int)(_t * 0.5f) % 4 == 3 ? 2 : 0);
                _lola.sprite = lolaFrames[frame];
            }
            Model.Step(Time.unscaledDeltaTime);
            float target = TargetCameraX();
            CameraX += (target - CameraX) * (1 - Mathf.Pow(1 - 0.12f, Time.unscaledDeltaTime * 60f));
            if (Mathf.Abs(target - CameraX) < 0.3f) CameraX = target;
            bool wasWalking = _wasWalking;
            _wasWalking = Model.Walking;
            if (wasWalking && !Model.Walking) Refresh(); else PlaceToken();
            if (_lolaMark.activeSelf) _lolaMark.transform.localPosition = new Vector3(0, Mathf.Round(Mathf.Sin(_t * 6f) * 1.5f) / 16f, 0);

            if (_firstMeetingAt > 0 && Time.unscaledTime >= _firstMeetingAt && !UiGate.Blocked && UiNavigator.Top == screen)
            {
                _firstMeetingAt = -1;
                if (!_save.metLola) TalkToLola();
            }

            var input = Root != null ? Root.Input : null;
            if (input == null || UiGate.Blocked || UiNavigator.Top != screen || Entering) return;
            var es = EventSystem.current;
            var sel = es != null ? es.currentSelectedGameObject : null;
            if (sel != null && sel.activeInHierarchy)
            {
                // a top-bar or Enter button has focus: the event system navigates; Down returns to the nodes
                if (input.Navigate.y < -0.5f && sel.transform.IsChildOf(screen.topBar)) es.SetSelectedGameObject(null);
                return;
            }
            int dx = input.NavigateXPressed;
            if (dx != 0) { if (Model.Select(Model.Selected + dx)) { UiAudio.Cue("select"); Refresh(); } }
            else if (input.Navigate.y > 0.5f && es != null) es.SetSelectedGameObject(screen.talk.gameObject);
            else if (input.SubmitPressed) Enter();
        }

        bool _wasWalking;

        void OnTap(Vector2 screenPos)
        {
            if (UiGate.Blocked || UiNavigator.Top != screen || Entering || cam == null) return;
            var world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
            var px = MapModel.ToMap(world);
            if (MapModel.LolaAt(px)) { TalkToLola(); return; }
            TapNode(MapModel.NodeAt(px));
        }

        /// <summary>Tap logic for a node index (tests call it directly): locked peeks, selected enters, other walks.</summary>
        public void TapNode(int i)
        {
            if (i < 0) return;
            if (Model.State(i) == NodeState.Locked)
            {
                Model.PeekLocked(i);
                UiAudio.Cue("locked");
                Refresh();
                return;
            }
            if (i == Model.Selected && Model.At == Model.Selected && !Model.Walking) Enter();
            else if (Model.Select(i)) { UiAudio.Cue("select"); Refresh(); }
        }

        public void Step(int dir)
        {
            if (Model.Select(Model.Selected + dir)) Refresh();
        }

        /// <summary>Enters the selected level (once: a second press during the wipe is ignored).</summary>
        public bool Enter()
        {
            if (Entering || UiGate.Blocked) return false;
            if (!Model.CanEnter)
            {
                if (Model.State(Model.Selected) == NodeState.Locked) UiAudio.Cue("locked");
                return false;
            }
            int i = Model.Selected;
            _save.diff = _diff;
            _save.cont = new ResumeState();
            Root.Save.Save();
            GameSession.Pending = new LevelRequest { Level = i, Difficulty = _diff };
            Vector2 focus = cam != null ? (Vector2)cam.WorldToViewportPoint(MapModel.ToWorld(MapModel.Nodes[i] + new Vector2(0, -8))) : new Vector2(0.5f, 0.5f);
            Entering = Root.Loader.Load(SceneNames.Level, focus);
            if (Entering) UiAudio.Cue("door");
            return Entering;
        }

        void CycleDifficulty()
        {
            int k = (System.Array.IndexOf(DiffCycle, _diff) + 1) % DiffCycle.Length;
            _diff = DiffCycle[k];
            _save.diff = _diff;
            Root.Save.Save();
            Refresh();
        }

        public void TalkToLola()
        {
            if (UiGate.Blocked || dialog.IsOpen) return;
            var keys = MapModel.LolaKeys(_save);
            var lines = new List<DialogLine>();
            foreach (var k in keys) lines.Add(new DialogLine { Who = Speaker.Lola, Name = Loc.Get("lola.name"), Sub = Loc.Get("lola.sub"), Text = Loc.Get(k) });
            LolaTalks++;
            UiAudio.Cue("select");
            dialog.Show(lines, () =>
            {
                _save.metLola = true;
                _save.lolaSeen = _save.unlocked;
                if (Root != null) Root.Save.Save();
                Refresh();
            }, _save.@char == "boy");
        }

        /// <summary>Opens the skill list (owned skills, and "?" with the level for the rest).</summary>
        public void ShowSkills()
        {
            if (skillsScreen == null || UiGate.Blocked || Entering) return;
            skillsScreen.Bind(_save);
            skillsScreen.BackRequested = () => skillsScreen.Close();
            skillsScreen.Open();
        }

        /// <summary>Edit-mode/audit helper: show Lola's dialog without the game root.</summary>
        public void ShowLolaForAudit()
        {
            var keys = MapModel.LolaKeys(_save);
            dialog.Show(new[] { new DialogLine { Who = Speaker.Lola, Name = Loc.Get("lola.name"), Sub = Loc.Get("lola.sub"), Text = Loc.Get(keys[0]) } }, null);
            dialog.CompleteLine();
        }
    }
}

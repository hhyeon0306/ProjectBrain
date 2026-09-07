using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAbilityKit.Unity
{
    /// <summary>프레임워크의 사용 예제. 게임 규칙 없이 입력·현재 상태·시각 반응만 담당한다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AbilityArenaDemo : MonoBehaviour
    {
        public AbilitySystemComponent hero;
        public AbilitySystemComponent dummy;
        private readonly Dictionary<string, Button> abilityButtons = new Dictionary<string, Button>();
        private Label resources, opponent, effects, history, resultLabel;
        private VisualElement panel;
        private ScrollView panelScroll;
        private VisualElement documentRoot;
        private Camera arenaCamera;
        private LineRenderer cue;
        private float cueUntil, phase;
        private readonly Color accent = new Color(.65f, .71f, 1f);
        private readonly Color foreground = new Color(.88f, .90f, .94f);

        private void Start()
        {
            if (hero == null || dummy == null) throw new InvalidOperationException("Arena requires two AbilitySystemComponents.");
            BuildPanel(); Refresh();
        }
        private void BuildPanel()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            documentRoot = root; arenaCamera = Camera.main;
            root.Clear(); root.style.unityFontDefinition = FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            panelScroll = new ScrollView(ScrollViewMode.Vertical) { name = "ability-arena-scroll" };
            panelScroll.style.position = Position.Absolute;
            panelScroll.style.left = panelScroll.style.top = panelScroll.style.bottom = 24;
            panelScroll.style.width = 400;
            panelScroll.style.maxWidth = Length.Percent(60);
            panelScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            root.Add(panelScroll);
            panel = new VisualElement { name = "ability-arena-panel" };
            panel.style.flexShrink = 0;
            panel.style.paddingLeft = panel.style.paddingRight = 22; panel.style.paddingTop = panel.style.paddingBottom = 18;
            panel.style.backgroundColor = new Color(.075f, .087f, .11f, .97f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 12;
            panelScroll.Add(panel);
            LabelText("UNITY ABILITY KIT", 12, accent);
            LabelText("Ability Lab", 28, Color.white).style.marginBottom = 8;
            LabelText("Data-defined skills / isolated actor state", 12, foreground).style.marginBottom = 16;
            resources = LabelText("", 15, foreground); opponent = LabelText("", 15, foreground);
            opponent.style.marginBottom = 12;
            abilityButtons.Clear();
            foreach (var ability in hero.System.GrantedAbilities)
            {
                string id = ability.Id;
                var button = ActionButton(ability.DisplayName, () => Activate(hero, id, dummy));
                abilityButtons.Add(id, button);
            }
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; panel.Add(row);
            var hit = ActionButton("Dummy attack", () => Activate(dummy, "Ability.Strike", hero));
            var stun = ActionButton("Stun hero", () => Activate(dummy, "Ability.Stun", hero));
            hit.RemoveFromHierarchy(); stun.RemoveFromHierarchy(); row.Add(hit); row.Add(stun); hit.style.flexGrow = stun.style.flexGrow = 1; stun.style.marginLeft = 6;
            resultLabel = LabelText("Choose a skill. Failed attempts spend nothing.", 12, accent);
            resultLabel.style.marginTop = 10;
            effects = LabelText("", 12, foreground); effects.style.marginTop = 8;
            history = LabelText("", 11, new Color(.61f, .66f, .74f)); history.style.marginTop = 12;
            ActionButton("Reset actors", ResetActors).style.marginTop = 10;
        }
        private Label LabelText(string text, int size, Color color)
        {
            var label = new Label(text); label.style.fontSize = size; label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal; label.style.marginBottom = 4; panel.Add(label); return label;
        }
        private Button ActionButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.height = 39; button.style.marginBottom = 5; button.style.fontSize = 12;
            button.style.color = foreground; button.style.backgroundColor = new Color(.16f, .18f, .23f);
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 6;
            panel.Add(button); return button;
        }
        public ActivationResult Activate(AbilitySystemComponent source, string id, AbilitySystemComponent target)
        {
            var result = source.System.TryActivate(id, target.System);
            if (resultLabel != null) resultLabel.text = source.actorId + " / " + id.Replace("Ability.", "") + " / " + (result.Success ? "COMMITTED" : Friendly(result.Failure));
            if (result.Success && Application.isPlaying)
            {
                var definition = source.System.GrantedAbilities.First(a => a.Id == id);
                var receiver = definition.Target == AbilityTarget.Self ? source : target;
                ShowCue(source.transform.position, receiver.transform.position, id.Contains("Heal") ? new Color(.4f, 1f, .7f) : accent);
            }
            Refresh(); return result;
        }
        public void ResetActors()
        {
            hero.RebuildSystem(); dummy.RebuildSystem();
            if (resultLabel != null) resultLabel.text = "Actors reset. Definitions unchanged.";
            Refresh();
        }
        private void Update()
        {
            if (hero?.System == null || resources == null) return;
            phase += Time.deltaTime * hero.System.Attributes.Get(AttributeId.MoveSpeed) * .25f;
            var position = hero.transform.position; position.z = Mathf.Sin(phase) * .55f; hero.transform.position = position;
            if (cue != null) cue.enabled = Time.time < cueUntil;
            Refresh();
        }
        private void LateUpdate()
        {
            if (arenaCamera == null || documentRoot == null || panelScroll == null) return;
            float width = documentRoot.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0) return;
            // Reserve the actual panel width, then fit both actors inside the remaining camera region.
            float left = Mathf.Clamp((panelScroll.worldBound.xMax + 18) / width, .2f, .75f);
            arenaCamera.rect = new Rect(left, 0, 1 - left, 1);
            arenaCamera.orthographicSize = Mathf.Max(4.3f, 2.8f / Mathf.Max(.1f, arenaCamera.aspect));
        }
        private void Refresh()
        {
            if (resources == null || hero?.System == null || dummy?.System == null) return;
            var h = hero.System; var d = dummy.System;
            resources.text = $"MAGE   HP {h.Attributes.Get(AttributeId.Health):0}   MANA {h.Attributes.GetBase(AttributeId.Mana):0}   SPEED {h.Attributes.Get(AttributeId.MoveSpeed):0.0}";
            opponent.text = $"DUMMY   HP {d.Attributes.Get(AttributeId.Health):0}   MANA {d.Attributes.GetBase(AttributeId.Mana):0}";
            foreach (var ability in h.GrantedAbilities)
            {
                double cooldown = h.CooldownRemaining(ability.Id);
                abilityButtons[ability.Id].text = $"{ability.DisplayName}   /   {ability.Cost:0} mana   /   " + (cooldown > 0 ? $"{cooldown:0.0}s" : "READY");
            }
            effects.text = "Hero: " + EffectText(h) + "\nDummy: " + EffectText(d);
            history.text = string.Join("\n", h.Events.Concat(d.Events).OrderByDescending(e => e.Time).Take(4).Select(e => $"{e.Time:0.0}  {e.Actor}  {e.Result.AbilityId.Replace("Ability.", "")}  {(e.Result.Success ? "OK" : e.Result.Failure.ToString())}"));
        }
        private static string EffectText(AbilitySystem system) => system.ActiveEffects.Length == 0 ? "no active effects" : string.Join(", ", system.ActiveEffects.Select(e => $"{e.Name} {e.Remaining:0.0}s"));
        private static string Friendly(AbilityFailure reason) => reason switch
        {
            AbilityFailure.OnCooldown => "WAIT FOR COOLDOWN — no cost",
            AbilityFailure.InsufficientResource => "NOT ENOUGH MANA — no cost",
            AbilityFailure.BlockedByTag => "STUNNED — no cost",
            AbilityFailure.ActorDead => "ACTOR DOWN — reset to continue",
            _ => reason.ToString()
        };
        private void ShowCue(Vector3 from, Vector3 to, Color color)
        {
            if (cue == null)
            {
                cue = new GameObject("Ability cue").AddComponent<LineRenderer>();
                cue.transform.SetParent(transform); cue.positionCount = 2; cue.startWidth = .07f; cue.endWidth = .02f;
                cue.material = new Material(Shader.Find("Sprites/Default"));
            }
            if (from == to) to += Vector3.up * 1.5f;
            cue.SetPosition(0, from + Vector3.up * .5f); cue.SetPosition(1, to + Vector3.up * .5f);
            cue.startColor = cue.endColor = color; cue.enabled = true; cueUntil = Time.time + .24f;
        }
        private void OnDestroy() { if (cue != null) { Destroy(cue.material); Destroy(cue.gameObject); } }
    }
}

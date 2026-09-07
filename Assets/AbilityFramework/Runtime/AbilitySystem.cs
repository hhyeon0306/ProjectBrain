using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityAbilityKit
{
    /// <summary>단일 게임 스레드의 실행기. 검사→비용/쿨다운 확정→효과 적용 순서로 실패 시 소모를 막는다.</summary>
    public sealed class AbilitySystem
    {
        private sealed class ActiveEffect { public long Handle; public GameplayEffectDefinition Definition; public double ExpiresAt; }
        public const int MaximumActiveEffects = 64;
        public string ActorId { get; }
        public AttributeSet Attributes { get; }
        public GameplayTagSet Tags { get; } = new GameplayTagSet();
        private readonly IAbilityClock clock;
        private readonly Dictionary<string, GameplayAbilityDefinition> abilities = new Dictionary<string, GameplayAbilityDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> cooldowns = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<long, ActiveEffect> effects = new Dictionary<long, ActiveEffect>();
        private readonly Queue<AbilityEvent> events = new Queue<AbilityEvent>();
        private long nextEffect;
        private double lastTime;
        public AbilitySystem(string actorId, AttributeSet attributes, IAbilityClock clock)
        {
            if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException(nameof(actorId));
            ActorId = actorId; Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes)); this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Now();
        }
        private double Now()
        {
            double now = clock.Now;
            if (double.IsNaN(now) || double.IsInfinity(now) || now < lastTime) throw new InvalidOperationException("Ability clock must be finite, nonnegative and monotonic.");
            lastTime = now; return now;
        }
        public void Grant(GameplayAbilityDefinition ability)
        {
            if (ability == null) throw new ArgumentNullException(nameof(ability));
            if (abilities.ContainsKey(ability.Id)) throw new InvalidOperationException("Ability ID already granted: " + ability.Id);
            abilities.Add(ability.Id, ability);
        }
        public bool Revoke(string id) { cooldowns.Remove(id); return abilities.Remove(id); }
        public IReadOnlyList<GameplayAbilityDefinition> GrantedAbilities => abilities.Values.OrderBy(a => a.Id, StringComparer.Ordinal).ToArray();
        public IReadOnlyList<AbilityEvent> Events => events.ToArray();
        public bool HasTag(string tag) => Tags.Has(tag) || Attributes.Contains(AttributeId.Health) && Attributes.Get(AttributeId.Health) <= 0 && GameplayTagSet.Matches("State.Dead", tag);
        public double CooldownRemaining(string id) => cooldowns.TryGetValue(id, out double end) ? Math.Max(0, end - Now()) : 0;
        public ActiveEffectView[] ActiveEffects => effects.Values.OrderBy(e => e.Handle).Select(e => new ActiveEffectView(e.Handle, e.Definition.DisplayName, Math.Max(0, e.ExpiresAt - Now()))).ToArray();
        public void Tick()
        {
            double now = Now();
            foreach (var handle in effects.Values.Where(e => e.ExpiresAt <= now).Select(e => e.Handle).ToArray()) RemoveEffect(handle);
        }
        public bool RemoveEffect(long handle)
        {
            if (!effects.TryGetValue(handle, out var effect)) return false;
            Attributes.RemoveModifiers(handle);
            foreach (var tag in effect.Definition.GrantedTags) Tags.Remove(tag);
            effects.Remove(handle); return true;
        }
        private AbilityFailure CanReceive(IEnumerable<GameplayEffectDefinition> definitions)
        {
            var values = definitions.ToArray();
            if (values.Any(e => e.Modifiers.Any(m => !Attributes.Contains(m.Attribute)))) return AbilityFailure.MissingAttribute;
            if (effects.Count + values.Count(e => !e.IsInstant) > MaximumActiveEffects) return AbilityFailure.EffectCapacity;
            return AbilityFailure.None;
        }
        public bool TryApplyEffect(GameplayEffectDefinition effect, out long handle)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            Tick(); handle = 0;
            if (CanReceive(new[] { effect }) != AbilityFailure.None) return false;
            handle = ApplyKnownEffect(effect); return true;
        }
        private long ApplyKnownEffect(GameplayEffectDefinition definition)
        {
            if (definition.IsInstant) { foreach (var modifier in definition.Modifiers) Attributes.ApplyInstant(modifier); return 0; }
            long handle = checked(++nextEffect);
            effects.Add(handle, new ActiveEffect { Handle = handle, Definition = definition, ExpiresAt = Now() + definition.Duration });
            Attributes.AddModifiers(handle, definition.Modifiers);
            foreach (var tag in definition.GrantedTags) Tags.Add(tag);
            return handle;
        }
        public ActivationResult CanActivate(string id, AbilitySystem target = null)
        {
            Tick(); target?.Tick();
            if (id == null || !abilities.TryGetValue(id, out var ability)) return new ActivationResult(id ?? "", AbilityFailure.NotGranted);
            var receiver = ability.Target == AbilityTarget.Self ? this : target;
            AbilityFailure failure;
            if (receiver == null || ability.Target == AbilityTarget.Other && ReferenceEquals(receiver, this)) failure = AbilityFailure.InvalidTarget;
            else if (HasTag("State.Dead")) failure = AbilityFailure.ActorDead;
            else if (ability.RequiredTags.Any(t => !HasTag(t))) failure = AbilityFailure.MissingRequiredTag;
            else if (ability.BlockedTags.Any(HasTag)) failure = AbilityFailure.BlockedByTag;
            else if (CooldownRemaining(id) > 0) failure = AbilityFailure.OnCooldown;
            else if (ability.Cost > 0 && !Attributes.CanSpend(ability.CostAttribute, ability.Cost)) failure = AbilityFailure.InsufficientResource;
            else failure = receiver.CanReceive(ability.Effects);
            return new ActivationResult(id, failure);
        }
        public ActivationResult TryActivate(string id, AbilitySystem target = null)
        {
            var result = CanActivate(id, target);
            var receiver = target;
            if (result.Success)
            {
                var ability = abilities[id]; receiver = ability.Target == AbilityTarget.Self ? this : target;
                // Definitions are immutable, all receivers were checked, and no external callback runs during commit.
                if (ability.Cost > 0) Attributes.Spend(ability.CostAttribute, ability.Cost);
                cooldowns[id] = Now() + ability.Cooldown;
                foreach (var effect in ability.Effects) receiver.ApplyKnownEffect(effect);
            }
            events.Enqueue(new AbilityEvent(Now(), ActorId, receiver?.ActorId ?? "", result));
            while (events.Count > 64) events.Dequeue();
            return result;
        }
    }
}

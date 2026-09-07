using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityAbilityKit
{
    public enum AbilityTarget { Self, Other }
    public enum AbilityFailure { None, NotGranted, InvalidTarget, ActorDead, MissingRequiredTag, BlockedByTag, OnCooldown, InsufficientResource, MissingAttribute, EffectCapacity }
    public readonly struct ActivationResult
    {
        public readonly string AbilityId;
        public readonly AbilityFailure Failure;
        public bool Success => Failure == AbilityFailure.None;
        public ActivationResult(string id, AbilityFailure failure) { AbilityId = id; Failure = failure; }
    }
    public readonly struct AbilityEvent
    {
        public readonly double Time;
        public readonly string Actor;
        public readonly string Target;
        public readonly ActivationResult Result;
        public AbilityEvent(double time, string actor, string target, ActivationResult result) { Time = time; Actor = actor; Target = target; Result = result; }
    }
    /// <summary>스킬 설정은 공유하고 자원·쿨다운·효과 상태는 소유자 AbilitySystem에 둔다.</summary>
    public sealed class GameplayAbilityDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public AttributeId CostAttribute { get; }
        public float Cost { get; }
        public double Cooldown { get; }
        public AbilityTarget Target { get; }
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> BlockedTags { get; }
        public IReadOnlyList<GameplayEffectDefinition> Effects { get; }
        public GameplayAbilityDefinition(string id, string displayName, float cost, double cooldown, AbilityTarget target, IEnumerable<GameplayEffectDefinition> effects, IEnumerable<string> requiredTags = null, IEnumerable<string> blockedTags = null, AttributeId costAttribute = AttributeId.Mana)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) || !AttributeDefinition.Finite(cost) || cost < 0 || double.IsNaN(cooldown) || double.IsInfinity(cooldown) || cooldown < 0 || !Enum.IsDefined(typeof(AbilityTarget), target) || !Enum.IsDefined(typeof(AttributeId), costAttribute))
                throw new ArgumentException("Invalid ability definition.");
            var values = (effects ?? Array.Empty<GameplayEffectDefinition>()).ToArray();
            if (values.Length == 0 || values.Any(e => e == null)) throw new ArgumentException("At least one effect is required.");
            Id = id; DisplayName = displayName; Cost = cost; Cooldown = cooldown; Target = target; CostAttribute = costAttribute;
            Effects = Array.AsReadOnly(values);
            RequiredTags = Array.AsReadOnly((requiredTags ?? Array.Empty<string>()).Select(GameplayTagSet.Validate).Distinct(StringComparer.Ordinal).ToArray());
            BlockedTags = Array.AsReadOnly((blockedTags ?? Array.Empty<string>()).Select(GameplayTagSet.Validate).Distinct(StringComparer.Ordinal).ToArray());
        }
    }
}

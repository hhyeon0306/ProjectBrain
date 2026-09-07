using System;
using System.Linq;
using UnityEngine;

namespace UnityAbilityKit.Unity
{
    [CreateAssetMenu(menuName = "Ability Kit/Gameplay Ability")]
    public sealed class GameplayAbilityAsset : ScriptableObject
    {
        public string abilityId = "Ability.New";
        public string displayName = "New ability";
        public AttributeId costAttribute = AttributeId.Mana;
        [Min(0)] public float cost;
        [Min(0)] public float cooldown;
        public AbilityTarget target;
        public GameplayEffectAsset[] effects = Array.Empty<GameplayEffectAsset>();
        public string[] requiredTags = Array.Empty<string>();
        public string[] blockedTags = { "State.Stunned" };
        public GameplayAbilityDefinition Build() => new GameplayAbilityDefinition(abilityId, displayName, cost, cooldown, target,
            effects.Select(e => e != null ? e.Build() : throw new InvalidOperationException("Missing effect on " + name)), requiredTags, blockedTags, costAttribute);
    }
}

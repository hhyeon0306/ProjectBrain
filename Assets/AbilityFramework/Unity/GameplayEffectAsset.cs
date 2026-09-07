using System;
using System.Linq;
using UnityEngine;

namespace UnityAbilityKit.Unity
{
    [Serializable]
    public struct AttributeModifierData
    {
        public AttributeId attribute;
        public ModifierOperation operation;
        public float magnitude;
        public AttributeModifier Build() => new AttributeModifier(attribute, operation, magnitude);
    }
    [CreateAssetMenu(menuName = "Ability Kit/Gameplay Effect")]
    public sealed class GameplayEffectAsset : ScriptableObject
    {
        public string effectId = "Effect.New";
        public string displayName = "New effect";
        [Min(0)] public float duration;
        public AttributeModifierData[] modifiers = Array.Empty<AttributeModifierData>();
        public string[] grantedTags = Array.Empty<string>();
        public GameplayEffectDefinition Build() => new GameplayEffectDefinition(effectId, displayName, duration, modifiers.Select(m => m.Build()), grantedTags);
    }
}

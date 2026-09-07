using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityAbilityKit
{
    /// <summary>공유하는 불변 효과 정의. 적용 대상마다 별도 handle과 만료 시각을 가진다.</summary>
    public sealed class GameplayEffectDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public double Duration { get; }
        public bool IsInstant => Duration == 0;
        public IReadOnlyList<AttributeModifier> Modifiers { get; }
        public IReadOnlyList<string> GrantedTags { get; }
        public GameplayEffectDefinition(string id, string displayName, double duration, IEnumerable<AttributeModifier> modifiers, IEnumerable<string> grantedTags = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) || double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0)
                throw new ArgumentException("Invalid effect identity/duration.");
            var values = (modifiers ?? Array.Empty<AttributeModifier>()).ToArray();
            var tags = (grantedTags ?? Array.Empty<string>()).Select(GameplayTagSet.Validate).Distinct(StringComparer.Ordinal).ToArray();
            if (values.Any(m => m == null) || values.Length + tags.Length == 0 || duration == 0 && tags.Length > 0)
                throw new ArgumentException("Effects need modifiers/tags; instant effects cannot grant lasting tags.");
            Id = id; DisplayName = displayName; Duration = duration;
            Modifiers = Array.AsReadOnly(values); GrantedTags = Array.AsReadOnly(tags);
        }
    }
    public readonly struct ActiveEffectView
    {
        public readonly long Handle;
        public readonly string Name;
        public readonly double Remaining;
        public ActiveEffectView(long handle, string name, double remaining) { Handle = handle; Name = name; Remaining = remaining; }
    }
}

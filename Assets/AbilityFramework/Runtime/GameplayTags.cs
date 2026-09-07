using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityAbilityKit
{
    /// <summary>점으로 구분한 상태 계층. State.Stunned는 State 조회에도 일치한다.</summary>
    public sealed class GameplayTagSet
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        public string[] Snapshot() => counts.Keys.OrderBy(t => t, StringComparer.Ordinal).ToArray();
        public static string Validate(string tag)
        {
            if (tag == null || tag.Length > 120 || !Regex.IsMatch(tag, "\\A[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*\\z"))
                throw new ArgumentException("Tag must be dot-separated identifiers.", nameof(tag));
            return tag;
        }
        public static bool Matches(string owned, string query) => owned == query || owned.StartsWith(query + ".", StringComparison.Ordinal);
        public bool Has(string query) { Validate(query); return counts.Keys.Any(t => Matches(t, query)); }
        public void Add(string tag) { Validate(tag); counts[tag] = checked(counts.TryGetValue(tag, out int count) ? count + 1 : 1); }
        public bool Remove(string tag)
        {
            Validate(tag);
            if (!counts.TryGetValue(tag, out int count)) return false;
            if (count == 1) counts.Remove(tag); else counts[tag] = count - 1;
            return true;
        }
    }
}

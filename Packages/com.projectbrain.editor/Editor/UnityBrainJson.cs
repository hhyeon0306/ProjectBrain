using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

namespace ProjectBrain
{
    // Only the serializer adapter depends on Unity. Storage and domain rules do not.
    public sealed class UnityBrainJson : IBrainJson
    {
        public string Write<T>(T value) => JsonUtility.ToJson(value, true);
        public T Read<T>(string json)
        {
            try
            {
                using (var parsed = JsonDocument.Parse(json)) ValidateShape(parsed.RootElement, typeof(T), "$");
                return JsonUtility.FromJson<T>(json);
            }
            catch (JsonException e) { throw new InvalidDataException("Brain JSON 구문이 잘못됐습니다.", e); }
            catch (ArgumentException e) { throw new InvalidDataException("Brain JSON을 읽을 수 없습니다.", e); }
        }

        // Inspect the input before Unity fills absent fields with construction defaults.
        private static void ValidateShape(JsonElement value, Type type, string path)
        {
            if (type == typeof(string)) { Require(value.ValueKind == JsonValueKind.String, path); return; }
            if (type == typeof(int)) { Require(value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _), path); return; }
            if (type == typeof(bool)) { Require(value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False, path); return; }
            if (type.IsArray)
            {
                Require(value.ValueKind == JsonValueKind.Array, path);
                int index = 0;
                foreach (var item in value.EnumerateArray()) ValidateShape(item, type.GetElementType(), path + "[" + index++ + "]");
                return;
            }
            Require(value.ValueKind == JsonValueKind.Object, path);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                Require(names.Add(property.Name), path + "." + property.Name + " (중복)");
                var field = type.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) continue;
                // Legacy documents allow absent/null optional text and arrays; v1 compatibility.
                if (type == typeof(ScriptDocument) && property.Name != "schemaVersion" && property.Name != "scriptGuid" && property.Value.ValueKind == JsonValueKind.Null) continue;
                ValidateShape(property.Value, field.FieldType, path + "." + property.Name);
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                bool optional = type == typeof(BrainTaskRecord) && field.Name == "scopeChanges"
                    || type == typeof(ScriptDocument) && field.Name != "schemaVersion" && field.Name != "scriptGuid"
                    || type == typeof(BrainNode) && (field.Name == "assetGuid" || field.Name == "lastKnownPath");
                if (!optional) Require(names.Contains(field.Name), path + "." + field.Name + " (필수)");
            }
        }

        private static void Require(bool valid, string path)
        {
            if (!valid) throw new InvalidDataException("Brain JSON 필드 누락 또는 형식 오류: " + path);
        }
    }
}

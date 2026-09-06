using System;
using System.IO;
using UnityEngine;

namespace ProjectBrain
{
    // Only the serializer adapter depends on Unity. Storage and domain rules do not.
    public sealed class UnityBrainJson : IBrainJson
    {
        public string Write<T>(T value) => JsonUtility.ToJson(value, true);
        public T Read<T>(string json)
        {
            try { return JsonUtility.FromJson<T>(json); }
            catch (ArgumentException e) { throw new InvalidDataException("Brain JSON을 읽을 수 없습니다.", e); }
        }
    }
}

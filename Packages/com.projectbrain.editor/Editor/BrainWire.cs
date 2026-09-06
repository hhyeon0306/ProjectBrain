using System.Text.Json;

namespace ProjectBrain
{
    public static class BrainWire
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);
        public static JsonElement Parse(string value) { using (var document = JsonDocument.Parse(value)) return document.RootElement.Clone(); }
        public static JsonElement Value<T>(T value) => Parse(Encode(value));
        // Ivan wraps the tool value in {"result":...}; count that JSON envelope too.
        public const int EnvelopeChars = 11;
    }
}

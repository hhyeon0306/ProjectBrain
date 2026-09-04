using System;

namespace ProjectBrain
{
    [Serializable]
    public sealed class ScriptDocument
    {
        public int schemaVersion = 2;
        public string scriptGuid;
        public string lastKnownPath;
        public string role = "";
        public string designIntent = "";
        public string cautions = "";
        public string body = "";
        public string[] imageGuids = Array.Empty<string>();
        public string[] relatedScriptGuids = Array.Empty<string>();
        public string savedCodeHash;
        public string updatedUtc;
    }
}

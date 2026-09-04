using System;

namespace ProjectBrain
{
    [Serializable]
    public sealed class ScriptDocument
    {
        public int schemaVersion = 1;
        public string scriptGuid;
        public string lastKnownPath;
        public string role = "";
        public string designIntent = "";
        public string cautions = "";
        public string[] relatedScriptGuids = Array.Empty<string>();
        public string savedCodeHash;
        public string updatedUtc;
    }
}

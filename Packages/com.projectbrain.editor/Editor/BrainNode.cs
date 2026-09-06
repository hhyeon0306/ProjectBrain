using System;

namespace ProjectBrain
{
    [Serializable]
    public sealed class BrainNode
    {
        public int schemaVersion = 1;
        public string id;
        public string type;
        public string title;
        public string summary = "";
        public string body = "";
        public string assetGuid = "";
        public string lastKnownPath = "";
        public string[] tags = Array.Empty<string>();
        public string status = "unreviewed";
        public string updatedUtc;
    }

    [Serializable]
    public sealed class BrainRelation
    {
        public string id;
        public string from;
        public string to;
        public string type;
        public string source;
        public string createdUtc;
    }

    [Serializable]
    public sealed class BrainRelationFile
    {
        public int schemaVersion = 1;
        public BrainRelation[] relations = Array.Empty<BrainRelation>();
    }

    public interface IBrainJson
    {
        string Write<T>(T value);
        T Read<T>(string json);
    }
}

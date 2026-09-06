using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectBrain
{
    // File persistence has no dependency on AssetDatabase, UI or MCP.
    public sealed class DocumentStore
    {
        private readonly string directory;
        public DocumentStore(string directory) => this.directory = Path.GetFullPath(directory);

        public ScriptDocument Load(string guid)
        {
            var path = GetPath(guid);
            if (!File.Exists(path)) return null;
            ScriptDocument document;
            try
            {
                document = new UnityBrainJson().Read<ScriptDocument>(File.ReadAllText(path));
                Validate(document, guid);
            }
            catch (InvalidDataException e) { throw new InvalidDataException(path + ": " + e.Message, e); }
            return document;
        }

        public void Save(ScriptDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            Validate(document, document.scriptGuid);
            var path = GetPath(document.scriptGuid);
            // Never silently overwrite an unreadable or future-schema document.
            if (File.Exists(path)) Load(document.scriptGuid);
            Directory.CreateDirectory(directory);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(document, true), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public IReadOnlyList<ScriptDocument> LoadAll()
        {
            if (!Directory.Exists(directory)) return Array.Empty<ScriptDocument>();
            var documents = new List<ScriptDocument>();
            foreach (var path in Directory.GetFiles(directory, "*.json"))
                documents.Add(Load(Path.GetFileNameWithoutExtension(path)));
            return documents;
        }

        private string GetPath(string guid)
        {
            ValidateGuid(guid);
            return Path.Combine(directory, guid + ".json");
        }

        private static void ValidateGuid(string guid)
        {
            if (guid == null || !Regex.IsMatch(guid, "\\A[0-9a-f]{32}\\z"))
                throw new InvalidDataException("유효한 Unity 스크립트 GUID가 필요합니다.");
        }

        private static void Validate(ScriptDocument document, string guid)
        {
            ValidateGuid(guid);
            if (document == null || document.schemaVersion < 1 || document.schemaVersion > 2 || document.scriptGuid != guid)
                throw new InvalidDataException("문서 버전 또는 GUID가 일치하지 않습니다.");
            document.schemaVersion = 2;
            document.body = document.body ?? "";
            if (document.imageGuids == null) document.imageGuids = Array.Empty<string>();
            foreach (var image in document.imageGuids) ValidateGuid(image);
            if (document.relatedScriptGuids == null) document.relatedScriptGuids = Array.Empty<string>();
            foreach (var related in document.relatedScriptGuids) ValidateGuid(related);
        }
    }
}

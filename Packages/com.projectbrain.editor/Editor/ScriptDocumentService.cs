using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectBrain
{
    public sealed class ScriptDocumentService
    {
        private readonly DocumentStore store;
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        public ScriptDocumentService() : this(new DocumentStore(Path.Combine(ProjectRoot, ".projectbrain", "docs"))) { }
        public ScriptDocumentService(DocumentStore store) => this.store = store;

        public ScriptDocument LoadOrCreate(MonoScript script)
        {
            var path = AssetDatabase.GetAssetPath(script);
            if (script == null || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assets 안의 C# 스크립트를 선택하세요.");
            var guid = AssetDatabase.AssetPathToGUID(path);
            return store.Load(guid) ?? new ScriptDocument { scriptGuid = guid, lastKnownPath = path };
        }

        public string ResolvePath(ScriptDocument document) => AssetDatabase.GUIDToAssetPath(document.scriptGuid);

        public string[] GetGraphRelatedGuids(ScriptDocument document)
        {
            var outgoing = document.relatedScriptGuids ?? Array.Empty<string>();
            var incoming = store.LoadAll()
                .Where(candidate => candidate.scriptGuid != document.scriptGuid)
                .Where(candidate => (candidate.relatedScriptGuids ?? Array.Empty<string>()).Contains(document.scriptGuid))
                .Select(candidate => candidate.scriptGuid);
            return outgoing.Concat(incoming).Distinct().ToArray();
        }

        public void Save(ScriptDocument document)
        {
            var path = ResolvePath(document);
            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<MonoScript>(path) == null)
                throw new InvalidOperationException("연결된 스크립트가 없습니다. 문서를 저장하지 않았습니다.");
            document.lastKnownPath = path;
            document.savedCodeHash = GetCodeHash(path);
            document.updatedUtc = DateTime.UtcNow.ToString("O");
            store.Save(document);
        }

        public string GetCodeHash(string assetPath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(Path.Combine(ProjectRoot, assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}

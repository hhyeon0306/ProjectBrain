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
        private readonly BrainDocumentSync sync;
        public static event Action<string> Saved;
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        public ScriptDocumentService() : this(new DocumentStore(Path.Combine(ProjectRoot, ".projectbrain", "docs")))
        { sync = new BrainDocumentSync(Path.Combine(ProjectRoot, ".projectbrain"), new UnityBrainJson(), AssetDatabase.GUIDToAssetPath); }
        public ScriptDocumentService(DocumentStore store) => this.store = store;

        public ScriptDocument LoadOrCreate(MonoScript script)
        {
            var path = AssetDatabase.GetAssetPath(script);
            if (script == null || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assets 안의 C# 스크립트를 선택하세요.");
            var guid = AssetDatabase.AssetPathToGUID(path);
            sync?.Version(guid); // Finish an interrupted save before reading its source.
            var document = store.Load(guid) ?? new ScriptDocument { scriptGuid = guid, lastKnownPath = path };
            if (sync != null)
            {
                var node = new BrainStore(Path.Combine(ProjectRoot, ".projectbrain"), new UnityBrainJson()).LoadNode("document:" + guid);
                if (node != null && (node.summary != (document.role ?? "") || node.body != BrainDocumentSync.Body(document)))
                    throw new InvalidOperationException("Explorer에서 별도로 수정된 문서가 있습니다. 양쪽 내용을 먼저 비교·정리해야 합니다. 기존 내용은 덮어쓰지 않았습니다.");
            }
            return document;
        }

        public string Version(ScriptDocument document) => sync?.Version(document.scriptGuid) ?? "";

        public string ResolvePath(ScriptDocument document) => AssetDatabase.GUIDToAssetPath(document.scriptGuid);

        public string[] GetGraphRelatedGuids(ScriptDocument document)
        {
            sync?.Version(document.scriptGuid);
            var outgoing = document.relatedScriptGuids ?? Array.Empty<string>();
            var incoming = store.LoadAll()
                .Where(candidate => candidate.scriptGuid != document.scriptGuid)
                .Where(candidate => (candidate.relatedScriptGuids ?? Array.Empty<string>()).Contains(document.scriptGuid))
                .Select(candidate => candidate.scriptGuid);
            return outgoing.Concat(incoming).Distinct().ToArray();
        }

        public void Save(ScriptDocument document, string expectedVersion = null)
        {
            var path = ResolvePath(document);
            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<MonoScript>(path) == null)
                throw new InvalidOperationException("연결된 스크립트가 없습니다. 문서를 저장하지 않았습니다.");
            if (sync != null && string.IsNullOrEmpty(expectedVersion))
                throw new InvalidOperationException("저장 전 문서를 읽고 Version 값을 전달하세요. 확인하지 않은 Explorer 내용을 덮어쓸 수 없습니다.");
            var version = expectedVersion ?? "";
            var copy = new UnityBrainJson().Read<ScriptDocument>(JsonUtility.ToJson(document));
            copy.lastKnownPath = path;
            copy.savedCodeHash = GetCodeHash(path);
            var previous = store.Load(copy.scriptGuid);
            copy.updatedUtc = previous?.updatedUtc;
            if (previous == null || JsonUtility.ToJson(copy) != JsonUtility.ToJson(previous)) copy.updatedUtc = DateTime.UtcNow.ToString("O");
            if (string.IsNullOrEmpty(copy.updatedUtc)) copy.updatedUtc = DateTime.UtcNow.ToString("O");
            if (sync == null) store.Save(copy); else sync.Save(copy, version);
            document.lastKnownPath = copy.lastKnownPath; document.savedCodeHash = copy.savedCodeHash; document.updatedUtc = copy.updatedUtc;
            // UI listeners cannot turn a successful durable save into an apparent failure.
            foreach (Action<string> listener in (sync == null ? null : Saved?.GetInvocationList()) ?? Array.Empty<Delegate>())
                try { listener(document.scriptGuid); } catch (Exception e) { Debug.LogWarning("[Project Brain] 화면 갱신 실패: " + e.Message); }
        }

        public string GetCodeHash(string assetPath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(Path.Combine(ProjectRoot, assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}

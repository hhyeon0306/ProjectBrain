using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainDocumentSyncWrite
    {
        public string path;
        public bool existed;
        public string before;
        public string after;
    }
    [Serializable] public sealed class BrainDocumentSyncBatch
    {
        public int schemaVersion = 1;
        public string id;
        public string documentGuid;
        public BrainDocumentSyncWrite[] writes;
    }

    // A single Editor writer. A durable intent lets interrupted multi-file saves resume;
    // every old byte is retained in history, and concurrent changes stop recovery.
    public sealed class BrainDocumentSync
    {
        private readonly string root;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolve;
        private readonly Action<int> afterWrite;
        public BrainDocumentSync(string brainRoot, IBrainJson json, Func<string, string> resolve, Action<int> afterWrite = null)
        { root = Path.GetFullPath(brainRoot); this.json = json; this.resolve = resolve; this.afterWrite = afterWrite; }

        public static string Body(ScriptDocument d) => "## 역할\n" + d.role + "\n\n## 설계 의도\n" + d.designIntent + "\n\n## 주의사항\n" + d.cautions + "\n\n## 본문\n" + d.body + "\n\n## 원본 저장 코드 해시 (현재 검증 아님)\n" + d.savedCodeHash;
        public string Version(string guid)
        {
            DocumentStore.ValidateGuid(guid);
            Recover(root, json);
            var store = new BrainStore(root, json);
            var doc = store.LoadNode("document:" + guid);
            var path = Path.Combine(root, "docs", guid + ".json");
            var source = File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : "missing";
            return BrainWorkspace.Hash(source + "\n" + (doc == null ? "missing" : json.Write(doc)) + "\n" + string.Join("\n", store.LoadRelations().Where(BrainVerificationStore.Semantic).OrderBy(r => r.id, StringComparer.Ordinal).Select(r => json.Write(r))));
        }

        public void Save(ScriptDocument document, string expectedVersion)
        {
            var d = json.Read<ScriptDocument>(json.Write(document));
            DocumentStore.Validate(d, d.scriptGuid);
            BrainWorkspace.Require(Version(d.scriptGuid) == expectedVersion, "문서 또는 Explorer의 내용·연결이 변경됐습니다. 입력은 유지했습니다. 최신 내용을 다시 읽고 비교하세요.");
            var store = new BrainStore(root, json);
            var nodes = store.LoadNodes().ToDictionary(n => n.id, StringComparer.Ordinal);
            var relations = store.LoadRelations().ToList();
            var writes = new List<BrainDocumentSyncWrite>();
            Action<string, string> stage = (path, content) =>
            {
                var full = Path.Combine(root, path); bool exists = File.Exists(full);
                var before = exists ? Convert.ToBase64String(File.ReadAllBytes(full)) : "";
                var after = Convert.ToBase64String(new System.Text.UTF8Encoding(false).GetBytes(content));
                if (before != after) writes.Add(new BrainDocumentSyncWrite { path = path, existed = exists, before = before, after = after });
            };
            Action<BrainNode> node = value =>
            {
                BrainStore.ValidateNode(value);
                if (nodes.TryGetValue(value.id, out var old))
                    BrainWorkspace.Require(old.type == value.type && old.assetGuid == value.assetGuid, "연결 자산 종류가 충돌합니다: " + value.id);
                nodes[value.id] = value;
                stage("nodes/" + BrainStore.SafeId(value.id) + ".json", json.Write(value));
            };
            Func<string, string, BrainNode> asset = (guid, type) =>
            {
                DocumentStore.ValidateGuid(guid);
                if (nodes.TryGetValue("asset:" + guid, out var existing))
                { BrainWorkspace.Require(existing.type == type, "연결 자산 종류가 충돌합니다."); return existing; }
                var path = resolve(guid) ?? "";
                var value = new BrainNode { id = "asset:" + guid, assetGuid = guid, type = type, title = path.Length == 0 ? guid : Path.GetFileName(path), lastKnownPath = path, status = path.Length == 0 ? "missing" : "unreviewed", updatedUtc = d.updatedUtc };
                node(value); return value;
            };
            var code = asset(d.scriptGuid, "Code");
            var id = "document:" + d.scriptGuid;
            var target = nodes.TryGetValue(id, out var previous) ? json.Read<BrainNode>(json.Write(previous)) : new BrainNode { id = id, type = "Document", title = code.title + " Design" };
            BrainWorkspace.Require(target.type == "Document", "문서 ID 종류가 충돌합니다.");
            if (previous == null || target.summary != (d.role ?? "") || target.body != Body(d))
            { target.summary = d.role ?? ""; target.body = Body(d); target.status = "unreviewed"; target.updatedUtc = d.updatedUtc; }
            node(target);
            var desired = new List<BrainRelation>();
            Action<string, string, string> edge = (from, to, type) => desired.Add(new BrainRelation
            {
                id = "relation:" + BrainWorkspace.Hash("script-document\n" + from + "\n" + type + "\n" + to),
                from = from, to = to, type = type, source = "script-document", createdUtc = d.updatedUtc
            });
            edge(code.id, id, "documented_by");
            foreach (var guid in d.imageGuids.Distinct()) edge(id, asset(guid, "Image").id, "illustrated_by");
            foreach (var guid in d.relatedScriptGuids.Distinct())
            {
                BrainWorkspace.Require(guid != d.scriptGuid, "자기 자신을 연결할 수 없습니다.");
                edge(code.id, asset(guid, "Code").id, "depends_on");
            }
            Func<BrainRelation, bool> owned = r => (r.source == "script-document" || r.source == "v2-migration") &&
                (r.from == code.id && (r.type == "depends_on" || r.type == "documented_by" && r.to == id) || r.from == id && r.type == "illustrated_by");
            relations.RemoveAll(r => owned(r) && !desired.Any(w => SameEdge(w, r)));
            foreach (var relation in desired) if (!relations.Any(r => SameEdge(r, relation))) relations.Add(relation);
            store.ValidateRelations(relations.ToArray(), key => nodes.TryGetValue(key, out var value) ? value : null);
            stage("relations.json", json.Write(new BrainRelationFile { relations = relations.ToArray() }));
            stage("docs/" + d.scriptGuid + ".json", json.Write(d));
            if (writes.Count == 0) return;
            BrainWorkspace.Require(Version(d.scriptGuid) == expectedVersion, "저장 준비 중 문서가 변경됐습니다. 다시 읽으세요.");
            var batch = new BrainDocumentSyncBatch { id = Guid.NewGuid().ToString("D"), documentGuid = d.scriptGuid, writes = writes.ToArray() };
            BrainStore.AtomicWrite(Path.Combine(root, "document-sync/pending.json"), json.Write(batch));
            try { Recover(root, json, afterWrite); }
            catch (Exception e) { throw new IOException("문서 자동 반영을 끝내지 못했습니다. 변경 전 원본과 재개 기록을 보존했습니다. 다시 읽으면 안전하게 재개합니다. " + e.Message, e); }
        }
        private static bool SameEdge(BrainRelation a, BrainRelation b) => a.from == b.from && a.to == b.to && a.type == b.type;

        internal static void Recover(string brainRoot, IBrainJson json, Action<int> afterWrite = null)
        {
            var pending = Path.Combine(brainRoot, "document-sync/pending.json");
            if (!File.Exists(pending)) return;
            var batch = json.Read<BrainDocumentSyncBatch>(File.ReadAllText(pending));
            BrainWorkspace.Require(batch.schemaVersion == 1 && Guid.TryParseExact(batch.id, "D", out _) && batch.writes != null && batch.writes.Length > 0, "자동 반영 재개 기록이 손상됐습니다.");
            DocumentStore.ValidateGuid(batch.documentGuid);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            // Validate every file before resuming any write. Never resolve an external edit by overwriting it.
            foreach (var write in batch.writes)
            {
                BrainWorkspace.Require(write != null && write.path != null && seen.Add(write.path) &&
                    (write.path == "relations.json" || write.path == "docs/" + batch.documentGuid + ".json" || Regex.IsMatch(write.path, "\\Anodes/[0-9a-f]{64}\\.json\\z")), "잘못된 자동 반영 경로입니다.");
                Convert.FromBase64String(write.before); Convert.FromBase64String(write.after);
                var path = Path.Combine(brainRoot, write.path);
                bool exists = File.Exists(path); var current = exists ? Convert.ToBase64String(File.ReadAllBytes(path)) : "";
                BrainWorkspace.Require(current == write.after || exists == write.existed && current == write.before, "자동 반영 중 별도 변경이 발견됐습니다. 원본을 덮어쓰지 않습니다: " + write.path);
            }
            int count = 0;
            foreach (var write in batch.writes)
            {
                var path = Path.Combine(brainRoot, write.path);
                var bytes = Convert.FromBase64String(write.after);
                if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes))
                    BrainStore.AtomicWrite(path, new System.Text.UTF8Encoding(false, true).GetString(bytes));
                afterWrite?.Invoke(++count);
            }
            var history = Path.Combine(brainRoot, "document-sync/history", batch.id + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(history));
            File.Move(pending, history);
        }
    }
}

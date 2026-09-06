using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ProjectBrain
{
    [Serializable]
    public sealed class BrainMigrationReceipt
    {
        public int schemaVersion = 1;
        public string completedUtc;
        public BrainMigrationSource[] sources;
        public string[] nodeIds;
        public string[] relationIds;
    }

    [Serializable]
    public sealed class BrainMigrationSource
    {
        public string guid;
        public int originalVersion;
        public string sha256;
    }

    // Explicit, additive import. Old document UI may continue to use docs/;
    // source edits after a completed import require reconciliation, not silent overwrite.
    public sealed class BrainMigration
    {
        private readonly string root;
        private readonly IBrainJson json;
        private readonly Func<string, string> resolveAsset;
        public BrainMigration(string root, IBrainJson json, Func<string, string> resolveAsset)
        {
            this.root = Path.GetFullPath(root);
            this.json = json;
            this.resolveAsset = resolveAsset;
        }

        public BrainMigrationReceipt Run()
        {
            var docsRoot = Path.Combine(root, "docs");
            var sourcePaths = Directory.Exists(docsRoot) ? Directory.GetFiles(docsRoot, "*.json").OrderBy(p => p, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
            var sources = sourcePaths.Select(ReadSource).ToArray();
            var documents = new DocumentStore(docsRoot).LoadAll(); // Validate every source before writing anything.
            var store = new BrainStore(root, json);
            var existingNodes = store.LoadNodes().ToDictionary(n => n.id, StringComparer.Ordinal);
            var existingRelations = store.LoadRelations();
            var receiptPath = Path.Combine(root, "migration.json");
            if (File.Exists(receiptPath))
            {
                var previous = json.Read<BrainMigrationReceipt>(File.ReadAllText(receiptPath));
                Require(previous != null && previous.schemaVersion == 1 && previous.sources != null && previous.nodeIds != null && previous.relationIds != null && !string.IsNullOrEmpty(previous.completedUtc), "손상되었거나 지원하지 않는 migration.json입니다.");
                Require(SameSources(previous.sources, sources), "이관 후 원본 문서가 변경됐습니다. 자동 덮어쓰기 대신 명시적 재조정이 필요합니다.");
                Require(previous.nodeIds.All(id => id != null && existingNodes.ContainsKey(id)) && previous.relationIds.All(id => existingRelations.Any(r => r.id == id)), "이관 결과 노드 또는 관계가 삭제됐습니다.");
                return previous; // Preserve subsequent user edits to migrated nodes and relations.
            }

            var nodes = new Dictionary<string, BrainNode>(StringComparer.Ordinal);
            var relations = new Dictionary<string, BrainRelation>(StringComparer.Ordinal);
            foreach (var doc in documents.OrderBy(d => d.scriptGuid, StringComparer.Ordinal))
            {
                // Source timestamp makes an interrupted run deterministic.
                var timestamp = string.IsNullOrEmpty(doc.updatedUtc) ? "1970-01-01T00:00:00Z" : doc.updatedUtc;
                var code = AssetNode(doc.scriptGuid, "Code", timestamp, doc.lastKnownPath);
                code.summary = doc.role ?? "";
                AddNode(nodes, code, true);
                var document = new BrainNode
                {
                    id = "document:" + doc.scriptGuid,
                    type = "Document",
                    title = code.title + " Design",
                    summary = doc.role ?? "",
                    body = "## 역할\n" + doc.role + "\n\n## 설계 의도\n" + doc.designIntent + "\n\n## 주의사항\n" + doc.cautions + "\n\n## 본문\n" + doc.body + "\n\n## 원본 저장 코드 해시 (현재 검증 아님)\n" + doc.savedCodeHash,
                    tags = new[] { "migrated-v2" },
                    updatedUtc = timestamp
                };
                AddNode(nodes, document, true);
                AddRelation(relations, code.id, document.id, "documented_by", timestamp);
                foreach (var guid in doc.relatedScriptGuids)
                {
                    var related = AssetNode(guid, "Code", timestamp, "");
                    AddNode(nodes, related, false);
                    AddRelation(relations, code.id, related.id, "depends_on", timestamp);
                }
                foreach (var guid in doc.imageGuids)
                {
                    var image = AssetNode(guid, "Image", timestamp, "");
                    AddNode(nodes, image, false);
                    AddRelation(relations, document.id, image.id, "illustrated_by", timestamp);
                }
            }

            // Preflight all conflicts before the first destination write. Exact partial
            // imports are resumable; unrelated edits are never overwritten.
            foreach (var node in nodes.Values)
            {
                BrainStore.ValidateNode(node);
                if (existingNodes.TryGetValue(node.id, out var existing))
                    Require(json.Write(existing) == json.Write(node), "기존 노드와 이관 결과가 충돌합니다: " + node.id);
            }
            var merged = existingRelations.ToList();
            foreach (var relation in relations.Values)
            {
                var existing = merged.FirstOrDefault(r => r.id == relation.id || (r.from == relation.from && r.to == relation.to && r.type == relation.type));
                if (existing != null)
                    Require(json.Write(existing) == json.Write(relation), "기존 관계와 이관 결과가 충돌합니다: " + relation.id);
                else merged.Add(relation);
            }
            Require(SourcesUnchanged(sourcePaths, sources), "이관 준비 중 원본이 변경됐습니다.");
            foreach (var node in nodes.Values)
                if (!existingNodes.ContainsKey(node.id)) store.SaveNode(node);
            store.SaveRelations(merged);
            // Read back the persisted files before marking completion.
            foreach (var node in nodes.Values)
                Require(json.Write(store.LoadNode(node.id)) == json.Write(node), "노드 재읽기가 일치하지 않습니다: " + node.id);
            var reloaded = store.LoadRelations();
            foreach (var relation in relations.Values)
                Require(reloaded.Any(r => json.Write(r) == json.Write(relation)), "관계 재읽기가 일치하지 않습니다: " + relation.id);
            Require(SourcesUnchanged(sourcePaths, sources), "이관 중 원본이 변경됐습니다. 완료 기록은 생성하지 않습니다.");
            var receipt = new BrainMigrationReceipt
            {
                completedUtc = DateTime.UtcNow.ToString("O"), sources = sources,
                nodeIds = nodes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                relationIds = relations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray()
            };
            BrainStore.AtomicWrite(receiptPath, json.Write(receipt));
            return receipt;
        }

        private BrainNode AssetNode(string guid, string type, string timestamp, string fallback)
        {
            var resolved = resolveAsset(guid);
            var path = string.IsNullOrEmpty(resolved) ? fallback ?? "" : resolved;
            return new BrainNode
            {
                id = "asset:" + guid, type = type, assetGuid = guid, lastKnownPath = path,
                title = string.IsNullOrEmpty(path) ? guid : Path.GetFileName(path),
                status = string.IsNullOrEmpty(resolved) ? "missing" : "unreviewed",
                tags = new[] { "migrated-v2" }, updatedUtc = timestamp
            };
        }

        private static void AddNode(Dictionary<string, BrainNode> nodes, BrainNode node, bool primary)
        {
            if (nodes.TryGetValue(node.id, out var existing))
            {
                Require(existing.type == node.type, "하나의 GUID가 Code와 Image로 동시에 사용됐습니다: " + node.id);
                if (primary) nodes[node.id] = node;
            }
            else nodes.Add(node.id, node);
        }
        private static void AddRelation(Dictionary<string, BrainRelation> relations, string from, string to, string type, string timestamp)
        {
            var id = "relation:" + BrainStore.SafeId("migration:" + from.Replace(':', '/') + "/" + type + "/" + to.Replace(':', '/'));
            if (!relations.ContainsKey(id)) relations.Add(id, new BrainRelation { id = id, from = from, to = to, type = type, source = "v2-migration", createdUtc = timestamp });
        }
        private static bool SameSources(BrainMigrationSource[] a, BrainMigrationSource[] b) => a.Length == b.Length && a.Zip(b, (x, y) => x != null && x.guid == y.guid && x.originalVersion == y.originalVersion && x.sha256 == y.sha256).All(v => v);
        private BrainMigrationSource ReadSource(string path)
        {
            try
            {
                return new BrainMigrationSource { guid = Path.GetFileNameWithoutExtension(path), originalVersion = json.Read<ScriptDocument>(File.ReadAllText(path))?.schemaVersion ?? 0, sha256 = HashFile(path) };
            }
            catch (InvalidDataException e) { throw new InvalidDataException(path + ": " + e.Message, e); }
        }
        private bool SourcesUnchanged(string[] paths, BrainMigrationSource[] sources)
        {
            var docs = Path.Combine(root, "docs");
            var current = Directory.Exists(docs) ? Directory.GetFiles(docs, "*.json").OrderBy(p => p, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
            return paths.SequenceEqual(current) && paths.Select(HashFile).SequenceEqual(sources.Select(s => s.sha256));
        }
        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    }
}

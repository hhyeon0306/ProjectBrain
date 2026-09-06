using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectBrain
{
    public sealed class BrainStore
    {
        private readonly string root;
        private readonly IBrainJson json;
        private static readonly string[] NodeTypes = { "Project", "Domain", "Feature", "Code", "Document", "Image", "Evidence", "Activity", "Reference" };
        private static readonly string[] RelationTypes = { "contains", "depends_on", "implemented_by", "documented_by", "illustrated_by", "verified_by", "worked_on_in", "references" };

        public BrainStore(string root, IBrainJson json)
        {
            this.root = Path.GetFullPath(root);
            this.json = json ?? throw new ArgumentNullException(nameof(json));
        }

        // Hashing the complete ID avoids colon/slash, case and filename collisions on Windows.
        public static string SafeId(string id)
        {
            ValidateId(id);
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "").ToLowerInvariant();
        }

        private string NodePath(string id) => Path.Combine(root, "nodes", SafeId(id) + ".json");
        public BrainNode LoadNode(string id)
        {
            var path = NodePath(id);
            if (!File.Exists(path)) return null;
            var node = Read<BrainNode>(path);
            ValidateNode(node);
            Require(node.id == id, "노드 ID와 파일이 일치하지 않습니다: " + path);
            return node;
        }

        public IReadOnlyList<BrainNode> LoadNodes()
        {
            var directory = Path.Combine(root, "nodes");
            if (!Directory.Exists(directory)) return Array.Empty<BrainNode>();
            var nodes = new List<BrainNode>();
            foreach (var path in Directory.GetFiles(directory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var node = Read<BrainNode>(path);
                ValidateNode(node);
                Require(Path.GetFileName(path) == SafeId(node.id) + ".json", "노드 파일명이 ID와 일치하지 않습니다: " + path);
                nodes.Add(node);
            }
            return nodes;
        }

        public void SaveNode(BrainNode node)
        {
            ValidateNode(node);
            var previous = LoadNode(node.id); // Corrupt/future-schema files must survive failed writes.
            if (previous != null)
            {
                Require(previous.type == node.type && previous.assetGuid == node.assetGuid, "기존 노드 종류와 자산 ID는 변경할 수 없습니다.");
                Require(node.type != "Evidence" && node.type != "Activity", "증거·활동은 불변 기록입니다. 새 ID를 사용하세요.");
            }
            AtomicWrite(NodePath(node.id), json.Write(node));
        }

        public IReadOnlyList<BrainRelation> LoadRelations()
        {
            var path = Path.Combine(root, "relations.json");
            if (!File.Exists(path)) return Array.Empty<BrainRelation>();
            var file = Read<BrainRelationFile>(path);
            Require(file != null && file.schemaVersion == 1 && file.relations != null, "지원하지 않는 관계 파일입니다.");
            ValidateRelations(file.relations);
            return file.relations;
        }

        public void SaveRelations(IEnumerable<BrainRelation> relations)
        {
            if (relations == null) throw new ArgumentNullException(nameof(relations));
            var values = relations.ToArray();
            ValidateRelations(values);
            LoadRelations();
            AtomicWrite(Path.Combine(root, "relations.json"), json.Write(new BrainRelationFile { relations = values }));
        }

        public IReadOnlyList<BrainRelation> RelationsFor(string id)
        {
            Require(LoadNode(id) != null, "존재하지 않는 조회 노드: " + id);
            return LoadRelations().Where(r => r.from == id || r.to == id).ToArray();
        }

        private void ValidateRelations(BrainRelation[] relations)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (var relation in relations)
            {
                Require(relation != null, "빈 관계입니다.");
                ValidateId(relation.id);
                ValidateId(relation.from);
                ValidateId(relation.to);
                Require(ids.Add(relation.id), "중복 관계 ID: " + relation.id);
                Require(RelationTypes.Contains(relation.type), "알 수 없는 관계 종류: " + relation.type);
                Require(edges.Add(relation.from + "\n" + relation.type + "\n" + relation.to), "중복 관계입니다: " + relation.id);
                Require(!string.IsNullOrWhiteSpace(relation.source), "관계 출처가 필요합니다.");
                ValidateUtc(relation.createdUtc);
                Require(LoadNode(relation.from) != null && LoadNode(relation.to) != null, "관계 참조 대상이 없습니다: " + relation.id);
            }
        }

        public static void ValidateNode(BrainNode node)
        {
            Require(node != null && node.schemaVersion == 1, "지원하지 않는 노드 스키마입니다.");
            ValidateId(node.id);
            Require(NodeTypes.Contains(node.type), "알 수 없는 노드 종류: " + node.type);
            Require(!string.IsNullOrWhiteSpace(node.title), "노드 제목이 필요합니다: " + node.id);
            Require(node.summary != null && node.body != null && node.tags != null && node.tags.All(t => !string.IsNullOrWhiteSpace(t)), "본문·태그 형식이 잘못됐습니다: " + node.id);
            // A1 does not certify success. Verification statuses are introduced with evidence rules.
            Require(new[] { "unreviewed", "missing", "recorded" }.Contains(node.status), "지원하지 않는 상태입니다: " + node.status);
            ValidateUtc(node.updatedUtc);
            if (!string.IsNullOrEmpty(node.assetGuid))
            {
                Require(Regex.IsMatch(node.assetGuid, "\\A[0-9a-f]{32}\\z") && node.id == "asset:" + node.assetGuid, "자산 GUID와 ID가 일치하지 않습니다.");
                Require(node.type == "Code" || node.type == "Image", "자산 연결은 Code/Image에만 허용됩니다.");
            }
            else
            {
                Require(node.type != "Code" && node.type != "Image", "Code/Image에는 자산 GUID가 필요합니다.");
                Require(node.id.StartsWith(node.type.ToLowerInvariant() + ":", StringComparison.Ordinal), "노드 종류와 ID 접두사가 다릅니다.");
            }
            if (!string.IsNullOrEmpty(node.lastKnownPath))
                Require(!Path.IsPathRooted(node.lastKnownPath) && !node.lastKnownPath.Contains("\\") && !node.lastKnownPath.Split('/').Contains(".."), "자산 경로는 프로젝트 상대 경로여야 합니다.");
        }

        private T Read<T>(string path)
        {
            try { return json.Read<T>(File.ReadAllText(path)); }
            catch (InvalidDataException e) { throw new InvalidDataException(path + ": " + e.Message, e); }
        }

        private static void ValidateId(string id) => Require(id != null && id.Length <= 240 && Regex.IsMatch(id, "\\A[a-z][a-z0-9_-]*:[a-z0-9][a-z0-9_/-]*\\z"), "잘못된 Brain ID: " + id);
        private static void ValidateUtc(string value) => Require(value != null && value.EndsWith("Z", StringComparison.Ordinal) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "UTC 시각이 필요합니다.");
        private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }

        internal static void AtomicWrite(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, content, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}

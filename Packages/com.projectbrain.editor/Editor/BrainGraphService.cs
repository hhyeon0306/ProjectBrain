using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrain
{
    public sealed class BrainGraphService
    {
        public readonly Dictionary<string, BrainNode> Nodes;
        public readonly BrainRelation[] Relations;
        public static readonly string[] RelationOrder = { "contains", "implemented_by", "documented_by", "illustrated_by", "depends_on", "verified_by", "worked_on_in", "references" };
        public BrainGraphService(BrainStore store)
        {
            Nodes = store.LoadNodes().ToDictionary(n => n.id, StringComparer.Ordinal);
            Relations = store.LoadRelations().ToArray();
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in Relations)
            {
                var a = Nodes[r.from]; var b = Nodes[r.to];
                if (r.type == "contains")
                {
                    BrainWorkspace.Require((a.type == "Project" && b.type == "Domain") || (a.type == "Domain" && b.type == "Feature"), "잘못된 contains 계층: " + r.id);
                    BrainWorkspace.Require(parents.Add(b.id), "계층의 부모가 여러 개입니다: " + b.id);
                    if (b.type == "Feature") BrainWorkspace.Require(b.id.StartsWith("feature:" + a.id.Substring("domain:".Length) + "/", StringComparison.Ordinal), "Feature ID와 부모 Domain 불일치: " + b.id);
                }
                if (r.type == "implemented_by") BrainWorkspace.Require(a.type == "Feature" && b.type == "Code", "implemented_by 대상 오류: " + r.id);
                if (r.type == "documented_by") BrainWorkspace.Require((a.type == "Feature" || a.type == "Code") && b.type == "Document", "documented_by 대상 오류: " + r.id);
                if (r.type == "illustrated_by") BrainWorkspace.Require(a.type == "Document" && b.type == "Image", "illustrated_by 대상 오류: " + r.id);
            }
        }
        public BrainNode Get(string id) { BrainWorkspace.Require(id != null && Nodes.ContainsKey(id), "노드를 찾을 수 없습니다: " + id); return Nodes[id]; }
        public BrainRelation[] Around(string id) => Relations.Where(r => r.from == id || r.to == id).OrderBy(r => Array.IndexOf(RelationOrder, r.type)).ThenBy(r => r.id, StringComparer.Ordinal).ToArray();
        public string Parent(string id) => Relations.FirstOrDefault(r => r.type == "contains" && r.to == id)?.from;
        public string[] Children(string id) => Relations.Where(r => r.type == "contains" && r.from == id).OrderBy(r => r.to, StringComparer.Ordinal).Select(r => r.to).ToArray();
    }
}

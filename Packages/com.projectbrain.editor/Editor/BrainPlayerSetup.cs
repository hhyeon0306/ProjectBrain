using System;
using System.IO;
using System.Linq;

namespace ProjectBrain
{
    public static class BrainPlayerSetup
    {
        public static void AddHierarchy(string projectRoot)
        {
            var store = new BrainStore(Path.Combine(projectRoot, ".projectbrain"), new UnityBrainJson());
            var nodes = new[]
            {
                Node("project:brain-demo", "Project", "Project Brain 데모", "기존 데모 코드·설계 자료 탐색. 실행 가능한 게임 완성을 뜻하지 않습니다."),
                Node("domain:player", "Domain", "Player", "이동 입력·월드 이동·체력의 기존 예제 코드를 연결합니다."),
                Node("feature:player/movement", "Feature", "Movement", "입력 정규화와 월드 이동 예제. 입력 공급과 씬 연결은 별도입니다."),
                Node("feature:player/health", "Feature", "Health", "기존 체력 예제 코드와 설계 문서를 탐색합니다.")
            };
            var edges = new[]
            {
                Edge("project-player", nodes[0].id, nodes[1].id, "contains"),
                Edge("player-movement", nodes[1].id, nodes[2].id, "contains"),
                Edge("player-health", nodes[1].id, nodes[3].id, "contains"),
                Edge("movement-input", nodes[2].id, "asset:10000000000000000000000000000002", "implemented_by"),
                Edge("movement-code", nodes[2].id, "asset:10000000000000000000000000000003", "implemented_by"),
                Edge("health-code", nodes[3].id, "asset:10000000000000000000000000000004", "implemented_by")
            };
            var existing = store.LoadNodes().Select(n => n.id).ToHashSet();
            foreach (var edge in edges) BrainWorkspace.Require(existing.Contains(edge.to) || nodes.Any(n => n.id == edge.to), "데모 원본 노드가 없습니다: " + edge.to);
            var relations = store.LoadRelations().ToList();
            foreach (var node in nodes)
            {
                var previous = store.LoadNode(node.id);
                if (previous == null) store.SaveNode(node);
                else BrainWorkspace.Require(previous.type == node.type, "기존 데모 노드 종류 충돌: " + node.id);
            }
            foreach (var edge in edges)
            {
                var previous = relations.SingleOrDefault(r => r.id == edge.id);
                if (previous == null) relations.Add(edge);
                else BrainWorkspace.Require(previous.from == edge.from && previous.to == edge.to && previous.type == edge.type, "기존 데모 관계 충돌: " + edge.id);
            }
            store.SaveRelations(relations);
            new BrainGraphService(store);
        }
        private static BrainNode Node(string id, string type, string title, string summary) => new BrainNode { id = id, type = type, title = title, summary = summary, updatedUtc = "2026-09-06T00:00:00Z" };
        private static BrainRelation Edge(string id, string from, string to, string type) => new BrainRelation { id = "relation:a2-" + id, from = from, to = to, type = type, source = "manual-demo-mapping", createdUtc = "2026-09-06T00:00:00Z" };
    }
}

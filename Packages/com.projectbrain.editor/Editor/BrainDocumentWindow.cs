using System;
using UnityEditor;
using UnityEngine;

namespace ProjectBrain
{
    public sealed class BrainDocumentWindow : EditorWindow
    {
        private MonoScript selectedScript;
        private ScriptDocument document;
        private readonly ScriptDocumentService service = new ScriptDocumentService();
        private bool dirty;
        private string message;
        private Vector2 scroll;

        [MenuItem("Window/Project Brain/Script Document")]
        public static void Open() => GetWindow<BrainDocumentWindow>("Brain 문서");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("스크립트와 설계 문서", EditorStyles.boldLabel);
            var next = (MonoScript)EditorGUILayout.ObjectField("스크립트", selectedScript, typeof(MonoScript), false);
            if (next != selectedScript && (!dirty || EditorUtility.DisplayDialog("미저장 문서", "저장하지 않은 변경을 버리고 다른 스크립트를 열까요?", "변경 버리기", "취소")))
            {
                selectedScript = next;
                document = null;
                dirty = false;
                message = null;
                if (next != null) Try(() => document = service.LoadOrCreate(next));
            }
            if (document != null)
            {
                EditorGUILayout.LabelField("GUID", document.scriptGuid);
                var path = service.ResolvePath(document);
                EditorGUILayout.LabelField("연결 경로", string.IsNullOrEmpty(path) ? "연결된 코드 없음" : path);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("역할");
                document.role = EditorGUILayout.TextArea(document.role, GUILayout.MinHeight(55));
                EditorGUILayout.LabelField("설계 의도");
                document.designIntent = EditorGUILayout.TextArea(document.designIntent, GUILayout.MinHeight(75));
                EditorGUILayout.LabelField("주의사항");
                document.cautions = EditorGUILayout.TextArea(document.cautions, GUILayout.MinHeight(55));
                if (EditorGUI.EndChangeCheck()) dirty = true;
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("문서 저장은 코드 검증이나 검토 승인을 의미하지 않습니다.", MessageType.Info);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path)))
                {
                    if (GUILayout.Button(dirty ? "문서 저장 *" : "문서 저장")) Try(() => { service.Save(document); dirty = false; message = "저장했습니다: .projectbrain/docs/" + document.scriptGuid + ".json"; });
                    if (GUILayout.Button("연결된 코드 열기")) AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                }
                if (GUILayout.Button("저장된 문서 다시 읽기") && (!dirty || EditorUtility.DisplayDialog("미저장 문서", "변경을 버리고 다시 읽을까요?", "다시 읽기", "취소")))
                    Try(() => { document = service.LoadOrCreate(selectedScript); dirty = false; message = "다시 읽었습니다."; });
            }
            else EditorGUILayout.HelpBox("Project 창의 C# 스크립트를 위 필드에 넣으세요.", MessageType.Info);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
        }

        private void Try(Action action)
        {
            try { action(); }
            catch (Exception e) { message = e.Message; Debug.LogWarning("[Project Brain] " + e.Message); }
        }
    }
}

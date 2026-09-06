using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    // Shared presentation only. No task, document or approval state lives here.
    internal static class BrainTheme
    {
        internal const string Path = "Packages/com.projectbrain.editor/Editor/BrainTheme.uss";
        internal static readonly Color Accent = new Color32(166, 180, 235, 255);
        internal static void Apply(VisualElement root)
        {
            root.AddToClassList("brain");
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Path);
            if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
        }
        internal static Label Label(string text, string css = null)
        {
            var label = new Label(text);
            if (css != null) label.AddToClassList(css);
            return label;
        }
        internal static Button Button(string text, System.Action action, string name = null)
        {
            return new Button(action) { text = text, name = name, tooltip = text };
        }
        internal static void WrapField(TextField field, int minHeight = 72)
        {
            field.multiline = true;
            field.AddToClassList("stacked-field");
            field.style.whiteSpace = WhiteSpace.Normal;
            field.style.minWidth = 0;
            field.style.flexShrink = 0;
            field.style.alignSelf = Align.Stretch;
            field.style.width = StyleKeyword.Auto;
            var input = field.Q(className: "unity-base-text-field__input");
            if (input != null) { input.style.whiteSpace = WhiteSpace.Normal; input.style.minHeight = minHeight; input.style.minWidth = 0; }
            field.Query<TextElement>().ForEach(element => element.style.whiteSpace = WhiteSpace.Normal);
        }
        internal static string FreshnessName(string state)
        {
            switch (state) { case "current": return "저장 기준 일치"; case "stale": return "다시 확인 필요"; case "missing": return "자료 없음"; default: return "기준 미등록"; }
        }
        internal static string VerificationName(string state)
        {
            switch (state) { case "passed": return "통과"; case "failed": return "실패"; case "running": return "검증 중"; case "queued": return "대기"; case "interrupted": return "중단됨"; case "timeout": return "시간 초과"; default: return state; }
        }
        internal static string ReviewName(string state)
        {
            switch (state) { case "current": return "현재 내용 확인됨"; case "stale": return "변경 후 재확인 필요"; case "missing-code": return "연결 코드 없음"; case "unreviewed": return "아직 확인하지 않음"; default: return state; }
        }
        internal static string TypeName(string type)
        {
            switch (type)
            {
                case "Project": return "프로젝트"; case "Domain": return "도메인";
                case "Feature": return "기능"; case "Code": return "코드";
                case "Document": return "문서"; case "Image": return "이미지";
                case "Evidence": return "검증 기록"; case "Activity": return "작업 이력";
                default: return "참고 자료";
            }
        }
        internal static string RelationName(string type)
        {
            switch (type)
            {
                case "contains": return "포함"; case "implemented_by": return "구현";
                case "documented_by": return "설명"; case "illustrated_by": return "이미지";
                case "depends_on": return "의존"; case "verified_by": return "검증";
                case "worked_on_in": return "작업"; default: return "참조";
            }
        }
    }
}

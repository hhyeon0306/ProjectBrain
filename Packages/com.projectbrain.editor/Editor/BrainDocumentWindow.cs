using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectBrain
{
    public sealed class BrainDocumentWindow : EditorWindow
    {
        [SerializeField] private MonoScript selectedScript;
        [SerializeField] private ScriptDocument document;
        [SerializeField] private bool dirty;
        private readonly ScriptDocumentService service = new ScriptDocumentService();
        private VisualElement form;
        private Label identity;
        private HelpBox message;

        [MenuItem("Window/Project Brain/Script Document")]
        public static void Open() => GetWindow<BrainDocumentWindow>("Brain 문서");

        public void CreateGUI()
        {
            minSize = new Vector2(360, 420);
            var root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 12;
            var title = new Label("스크립트와 설계 문서");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;
            title.style.marginBottom = 12;
            root.Add(title);
            var picker = new ObjectField("스크립트") { objectType = typeof(MonoScript), allowSceneObjects = false };
            picker.SetValueWithoutNotify(selectedScript);
            picker.RegisterValueChangedCallback(evt =>
            {
                if (!ConfirmDiscard()) { picker.SetValueWithoutNotify(selectedScript); return; }
                selectedScript = evt.newValue as MonoScript;
                document = null;
                SetDirty(false);
                Run(() => { if (selectedScript != null) document = service.LoadOrCreate(selectedScript); });
                BuildForm();
            });
            root.Add(picker);
            form = new ScrollView();
            form.style.flexGrow = 1;
            root.Add(form);
            message = new HelpBox("C# 스크립트를 위 필드에 넣으세요.", HelpBoxMessageType.Info);
            root.Add(message);
            if (document == null && selectedScript != null) Run(() => document = service.LoadOrCreate(selectedScript));
            BuildForm();
            SetDirty(dirty);
        }

        private void BuildForm()
        {
            form.Clear();
            if (document == null) return;
            identity = new Label("GUID: " + document.scriptGuid + "\n" + service.ResolvePath(document));
            identity.style.whiteSpace = WhiteSpace.Normal;
            identity.style.marginTop = identity.style.marginBottom = 12;
            form.Add(identity);
            AddText("역할", document.role, value => document.role = value);
            AddText("설계 의도", document.designIntent, value => document.designIntent = value);
            AddText("주의사항", document.cautions, value => document.cautions = value);
            form.Add(new HelpBox("문서 저장은 코드 검증이나 검토 승인을 의미하지 않습니다.", HelpBoxMessageType.Info));
            form.Add(new Button(() => SaveChanges()) { text = "문서 저장" });
            form.Add(new Button(() => Run(() =>
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(service.ResolvePath(document));
                if (script == null) throw new InvalidOperationException("연결된 코드가 없습니다.");
                AssetDatabase.OpenAsset(script);
            })) { text = "연결된 코드 열기" });
            form.Add(new Button(() =>
            {
                if (!ConfirmDiscard()) return;
                Run(() => { document = service.LoadOrCreate(selectedScript); SetDirty(false); BuildForm(); message.text = "다시 읽었습니다."; });
            }) { text = "저장된 문서 다시 읽기" });
        }

        private void AddText(string label, string value, Action<string> assign)
        {
            form.Add(new Label(label));
            var field = new TextField { multiline = true, value = value ?? "" };
            field.style.minHeight = 64;
            field.style.marginBottom = 10;
            field.RegisterValueChangedCallback(evt => { assign(evt.newValue); SetDirty(true); });
            form.Add(field);
        }

        private bool ConfirmDiscard() => !dirty || EditorUtility.DisplayDialog("미저장 문서", "저장하지 않은 변경을 버릴까요?", "변경 버리기", "취소");
        private void SetDirty(bool value)
        {
            dirty = value;
            hasUnsavedChanges = value;
            saveChangesMessage = "Brain 문서의 변경을 저장할까요?";
        }
        public override void SaveChanges()
        {
            Run(() =>
            {
                if (document == null) return;
                service.Save(document);
                SetDirty(false);
                message.text = "저장했습니다: .projectbrain/docs/" + document.scriptGuid + ".json";
                message.messageType = HelpBoxMessageType.Info;
                identity.text = "GUID: " + document.scriptGuid + "\n" + service.ResolvePath(document);
            });
        }
        public override void DiscardChanges() { SetDirty(false); base.DiscardChanges(); }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { if (message != null) { message.text = e.Message; message.messageType = HelpBoxMessageType.Error; } Debug.LogWarning("[Project Brain] " + e.Message); }
        }
    }
}

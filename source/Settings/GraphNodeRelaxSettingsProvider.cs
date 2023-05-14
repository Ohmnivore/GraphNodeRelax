using UnityEditor;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    class GraphNodeRelaxSettingsProvider : SettingsProvider
    {
        public const string MenuPath = "Preferences/Graph Node Relax";

        SerializedObject m_ChangeTracker;

        public GraphNodeRelaxSettingsProvider() :
            base(MenuPath, SettingsScope.User)
        {

        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            m_ChangeTracker = new SerializedObject(Settings.instance);

            var editor = Editor.CreateEditor(Settings.instance);
            var gui = editor.CreateInspectorGUI();

            rootElement.Add(gui);
        }

        public override void OnInspectorUpdate()
        {
            base.OnInspectorUpdate();

            // We need to manually save the changes after modifications made in the UI
            if (m_ChangeTracker.UpdateIfRequiredOrScript())
                Settings.instance.SaveChanges();
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            var provider = new GraphNodeRelaxSettingsProvider();

            var serializedObject = new SerializedObject(Settings.instance);

            // Make searchable
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);

            return provider;
        }
    }
}

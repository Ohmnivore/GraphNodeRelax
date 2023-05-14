using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    [CustomEditor(typeof(Settings))]
    class SettingsEditor : Editor
    {
        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        VisualTreeAsset m_Layout;

        public override VisualElement CreateInspectorGUI()
        {
            // ScriptableSingleton sets this flag by default, which grays out PropertyFields
            target.hideFlags &= ~HideFlags.NotEditable;

            var rootElement = new VisualElement();

            m_Layout.CloneTree(rootElement);

            // Expand every PropertyField foldout
            for (var property = serializedObject.GetIterator(); property.Next(true); )
            {
                property.isExpanded= true;
            }

            rootElement.Bind(serializedObject);

            rootElement.Q<Button>("reset-button").clicked += () =>
            {
                var settings = target as Settings;
                settings.Reset();
            };

            return rootElement;
        }
    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using System.Reflection;

namespace GraphNodeRelax
{
    [InitializeOnLoad]
    class GraphNodeRelax
    {
        class WindowState
        {
            public GraphView GraphView;
            public GraphViewBrushManipulator Manipulator;
        }

        // The package's entry point
        static GraphNodeRelax()
        {
            var instance = new GraphNodeRelax();
            instance.OnEnable();
        }

        readonly Dictionary<EditorWindow, WindowState> m_RegisteredWindows = new Dictionary<EditorWindow, WindowState>();

        static readonly Dictionary<System.Type, System.Type> s_GraphToBuilder = TypeCache.GetTypesWithAttribute<GraphViewBuilderAttribute>()
            .Where(t => typeof(GraphView).IsAssignableFrom(t))
            .SelectMany(type => type.GetCustomAttributes(typeof(GraphViewBuilderAttribute)).Select(attribute => (type, attribute as GraphViewBuilderAttribute)))
            .Where(tuple => tuple.Item2 != null)
            .GroupBy(tuple => tuple.Item2.Type)
            .ToDictionary(t => t.Key, t => t.OrderByDescending(tuple => tuple.Item2.Priority).First().type);

        void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnUpdate()
        {
            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var window in allWindows)
            {
                // hasFocus actually means "is it visible"
                if (!window.hasFocus)
                    continue;

                if (m_RegisteredWindows.TryGetValue(window, out WindowState windowState))
                {
                    windowState.Manipulator.Update();
                }
                else if (window.rootVisualElement is { } root && root.Q<GraphView>() is { } graphView)
                {
                    // This window has a GraphView that we see for the first time

                    var graphNodeRelaxManipulator = new GraphViewBrushManipulator(ResolveBuilder(graphView));
                    graphView.AddManipulator(graphNodeRelaxManipulator);

                    m_RegisteredWindows.Add(window, new WindowState
                    {
                        GraphView = graphView,
                        Manipulator = graphNodeRelaxManipulator
                    });
                }
            }
        }

        void OnUndoRedo()
        {
            foreach (var (window, state) in m_RegisteredWindows)
            {
                if (window == null || !window.hasFocus)
                    continue;

                state.Manipulator.OnUndoRedo();
            }
        }

        IGraphViewBuilder ResolveBuilder(GraphView graph)
        {
            var type = graph.GetType();

            if (s_GraphToBuilder.TryGetValue(type, out var builderType))
            {
                return System.Activator.CreateInstance(builderType) as IGraphViewBuilder;
            }
            else
            {
                return new DefaultGraphViewBuilder();
            }
        }
    }
}

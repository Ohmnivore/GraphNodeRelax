using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    // Yet another Dragger implementation, meant to be used sporadically while in brush mode
    class GraphViewNodeDragger : Manipulator
    {
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active != value)
                {
                    m_Active = value;

                    m_Outline.Hide();

                    m_MouseHasBeenReleased = false;
                }
            }
        }

        bool m_Active;
        GraphElement m_Target;
        GraphViewChange m_GraphViewChange = new GraphViewChange { movedElements = new List<GraphElement>() };
        GraphElementOutline m_Outline = new GraphElementOutline();
        bool m_MouseHasBeenReleased;

        Rect m_OriginalPosition;
        Vector2 m_OriginalMouse;
        Vector2? m_MousePosition;

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException($"{nameof(GraphViewNodeDragger)} can only be added to a GraphView");
            }

            var trickleDown = TrickleDown.TrickleDown;

            target.RegisterCallback<MouseCaptureOutEvent>(OnSinkMouseCaptureOut);

            target.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), trickleDown);
            target.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), trickleDown);
            target.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), trickleDown);

            target.Add(m_Outline);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            var trickleDown = TrickleDown.TrickleDown;

            target.UnregisterCallback<MouseCaptureOutEvent>(OnSinkMouseCaptureOut);

            target.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), trickleDown);
            target.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), trickleDown);
            target.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), trickleDown);

            m_Outline.RemoveFromHierarchy();
        }

        void OnSinkMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            // The mouse capture has been released externally
            // (ex the user clicked inside a different window)

            if (!m_MouseHasBeenReleased)
            {
                target.CaptureMouse();
                m_MouseHasBeenReleased = true;
            }
            else
            {
                Active = false;
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            m_MousePosition = evt.mousePosition;

            if (!Active)
                return;

            m_Target = ResolveTarget(evt.mousePosition);
            if (m_Target != null)
            {
                m_GraphViewChange.movedElements.Clear();
                m_GraphViewChange.movedElements.Add(m_Target);

                var graphView = target as GraphView;
                var geometry = m_Target.GetPosition();
                m_OriginalPosition = m_Target.hierarchy.parent.ChangeCoordinatesTo(graphView.contentViewContainer, geometry);
            }

            m_OriginalMouse = evt.mousePosition;

            evt.StopImmediatePropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            m_MousePosition = evt.mousePosition;

            if (!Active)
                return;

            if (m_Target != null)
            {
                var graphView = target as GraphView;

                var mouseDelta = evt.mousePosition - m_OriginalMouse;

                Matrix4x4 g = m_Target.worldTransform;
                var scale = new Vector2(g.m00, g.m11);

                var newPosition = m_OriginalPosition;
                newPosition.position += mouseDelta / scale * new Vector2(m_Target.transform.scale.x, m_Target.transform.scale.y);
                m_Target.SetPosition(graphView.contentViewContainer.ChangeCoordinatesTo(m_Target.hierarchy.parent, newPosition));

                graphView.graphViewChanged?.Invoke(m_GraphViewChange);
            }

            evt.StopImmediatePropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            m_MousePosition = evt.mousePosition;

            if (!Active)
                return;

            m_Target = null;

            m_Outline.Hide();

            evt.StopImmediatePropagation();
        }

        GraphElement ResolveTarget(Vector2 mousePosition)
        {
            var pickedElement = target.panel.Pick(mousePosition);
            return ResolveTarget(pickedElement, mousePosition);
        }

        GraphElement ResolveTarget(VisualElement element, Vector2 mousePosition)
        {
            var graphElement = element as GraphElement;

            if (graphElement == null ||
                graphElement.IsStackable() ||
                !graphElement.IsMovable() ||
                !graphElement.HitTest(graphElement.WorldToLocal(mousePosition)))
            {
                var graphAncestor = element.GetFirstAncestorOfType<GraphElement>();
                if (graphAncestor == null)
                    return null;

                return ResolveTarget(graphAncestor, mousePosition);
            }

            return graphElement;
        }

        public void Update()
        {
            if (!Active)
                return;

            var graphView = target as GraphView;
            if (m_Target != null)
            {
                m_Outline.Update(graphView, m_Target, Settings.instance.Brush.ActiveColor);
            }
            else if (m_MousePosition is { } mousePosition &&
                     ResolveTarget(mousePosition) is { } hoveredElement)
            {
                m_Outline.Update(graphView, hoveredElement, Settings.instance.Brush.InactiveColor);
            }
            else
            {
                m_Outline.Hide();
            }
        }
    }
}

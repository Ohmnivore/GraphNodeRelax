using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    class GraphViewBrushManipulator : Manipulator
    {
        BrushCircle m_BrushCircle;
        BrushResizer m_BrushResizer;
        BrushCursor m_BrushCursor;
        ZoomerProxy m_Zoomer;
        GraphViewRelaxer m_Relaxer = new GraphViewRelaxer();
        IGraphViewBuilder m_GraphViewBuilder;
        GraphViewNodeDragger m_NodeDragger = new GraphViewNodeDragger();

        bool m_AttachedToPanel;
        bool m_Active;
        bool m_Brushing;
        bool m_BrushingMoving;
        bool m_HandlingMiddleMouse;
        bool m_HandlingContextualMenu;
        bool m_NeedsRefresh;

        // Called every frame
        public void Update()
        {
            m_BrushCursor.Update(target, m_Active);
            m_BrushResizer.Update(target, Settings.instance);
            UpdateBrushCircle(Settings.instance.Brush);

            if (m_Active && m_Brushing)
            {
                ApplyBrush(Settings.instance.Brush, Settings.instance.Algorithm);
            }

            m_NodeDragger.Update();
        }

        void UpdateBrushCircle(BrushSettings settings)
        {
            m_BrushCircle.Radius = settings.Radius;
            m_BrushCircle.Color = m_Brushing
                ? settings.ActiveColor
                : settings.InactiveColor;
        }

        void ApplyBrush(BrushSettings brushSettings, AlgorithmSettings algorithmSettings)
        {
            var graphView = target as GraphView;

            // Transform from window-space to graphview-space
            var brush = new Brush
            {
                Radius = brushSettings.Radius / graphView.contentViewContainer.transform.scale.x,
                Position = m_BrushCircle.ChangeCoordinatesTo(graphView.contentViewContainer, m_BrushCircle.Center)
            };

            if (m_NeedsRefresh)
            {
                m_Relaxer.Setup(graphView, m_GraphViewBuilder);
                m_NeedsRefresh = false;
            }

            m_Relaxer.Apply(brush, !m_BrushingMoving, algorithmSettings);
        }

        public void OnUndoRedo()
        {
            // Defer until all VisualElements are ready
            m_NeedsRefresh = true;
        }

        public GraphViewBrushManipulator(IGraphViewBuilder graphViewBuilder)
        {
            m_GraphViewBuilder = graphViewBuilder;

            m_BrushCircle = new BrushCircle();
            m_BrushCircle.style.position = Position.Absolute;
            m_BrushCircle.style.top = 0f;
            m_BrushCircle.style.left = 0f;
            m_BrushCircle.style.bottom = 0f;
            m_BrushCircle.style.right = 0f;
        }

        void Reset()
        {
            m_Active = false;
            m_Brushing = false;
            m_BrushingMoving = false;
            m_HandlingMiddleMouse = false;
            m_HandlingContextualMenu = false;
            m_BrushResizer.Reset();
            m_BrushCircle.RemoveFromHierarchy();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (target is not GraphView graphView)
            {
                throw new InvalidOperationException($"{nameof(GraphViewBrushManipulator)} can only be added to a GraphView");
            }

            m_BrushResizer = new BrushResizer();
            m_BrushCursor = new BrushCursor(target);
            m_Zoomer = new ZoomerProxy(graphView);

            if (target.panel != null && !m_AttachedToPanel)
            {
                OnAttachToPanel(target.panel);
            }

            var trickleDown = TrickleDown.TrickleDown;

            target.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            target.RegisterCallback<MouseCaptureOutEvent>(OnSinkMouseCaptureOut);

            target.RegisterCallback<MouseDownEvent>(OnMouseDown, trickleDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, trickleDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, trickleDown);

            target.RegisterCallback<WheelEvent>(OnWheel, trickleDown);

            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate, trickleDown);

            target.AddManipulator(m_NodeDragger);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            var trickleDown = TrickleDown.TrickleDown;

            target.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            target.UnregisterCallback<MouseCaptureOutEvent>(OnSinkMouseCaptureOut);

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, trickleDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp, trickleDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, trickleDown);

            target.UnregisterCallback<WheelEvent>(OnWheel, trickleDown);

            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate, trickleDown);

            target.RemoveManipulator(m_NodeDragger);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            OnAttachToPanel(evt.destinationPanel);
        }

        void OnAttachToPanel(IPanel panel)
        {
            if (m_AttachedToPanel)
                return;

            var trickleDown = TrickleDown.TrickleDown;

            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDown, trickleDown);
            panel.visualTree.RegisterCallback<KeyUpEvent>(OnKeyUp, trickleDown);

            Reset();
            m_AttachedToPanel = true;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            OnDetachFromPanel(evt.originPanel);
        }

        void OnDetachFromPanel(IPanel panel)
        {
            if (!m_AttachedToPanel)
                return;

            var trickleDown = TrickleDown.TrickleDown;

            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDown, trickleDown);
            panel.visualTree.UnregisterCallback<KeyUpEvent>(OnKeyUp, trickleDown);

            Reset();
            m_AttachedToPanel = false;
        }

        void Activate()
        {
            var graphView = target as GraphView;
            m_Relaxer.Setup(graphView, m_GraphViewBuilder);

            m_BrushCursor.ApplyCursor(target);

            m_Active = true;
            target.CaptureMouse();

            target.Add(m_BrushCircle);
        }

        void Deactivate(bool deactivateNodeDragger = true)
        {
            m_BrushCursor.RestoreCursor(target);

            m_Active = false;

            target.ReleaseMouse();

            if (deactivateNodeDragger)
                m_NodeDragger.Active = false;

            m_HandlingMiddleMouse = false;
            m_BrushCircle.RemoveFromHierarchy();

            m_Relaxer.RegisterUndo();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            var settings = Settings.instance.Shortcuts;

            if (settings.EnableBrush.Matches(evt) && !m_Active)
            {
                Activate();
            }
            else if (settings.DisableBrush.Matches(evt) && m_Active)
            {
                Deactivate();
            }
            else if (settings.EnableSingleNodeDragMode.Matches(evt) && m_Active && !m_NodeDragger.Active)
            {
                Deactivate();
                m_NodeDragger.Active = true;
            }
            else
            {
                m_BrushResizer.OnKeyDown(evt, Settings.instance.Shortcuts);
            }
        }

        void OnKeyUp(KeyUpEvent evt)
        {
            var settings = Settings.instance.Shortcuts;

            if (settings.EnableSingleNodeDragMode.WasReleased(evt) && m_NodeDragger.Active)
            {
                m_NodeDragger.Active = false;
                Activate();
            }
            else
            {
                m_BrushResizer.OnKeyUp(evt, Settings.instance.Shortcuts);
            }
        }

        void OnSinkMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            if (m_Active && m_HandlingContextualMenu)
            {
                m_HandlingContextualMenu = false;

                target.CaptureMouse();
            }
            else if (m_Active && m_HandlingMiddleMouse)
            {
                m_HandlingMiddleMouse = false;

                target.CaptureMouse();
            }
            else
            {
                // The mouse capture has been released externally
                // (ex the user clicked inside a different window)

                Deactivate(false);
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            m_BrushCircle.Center = evt.localMousePosition;

            if (!m_Active)
            {
                return;
            }

            if (evt.button == (int)MouseButton.MiddleMouse)
            {
                // Redirect to the GraphView.ContentDragger

                m_HandlingMiddleMouse = true;
                return;
            }

            if (evt.button == (int)MouseButton.RightMouse)
            {
                // Prevent right-click interaction
                // (together with OnContextualMenuPopulate)

                evt.StopImmediatePropagation();
                return;
            }

            m_Brushing = true;
            m_BrushingMoving = false;

            evt.StopImmediatePropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            m_BrushCircle.Center = evt.localMousePosition;

            if (!m_Active)
                return;

            if (evt.button == (int)MouseButton.MiddleMouse)
            {
                // Redirect to the GraphView.ContentDragger

                m_HandlingMiddleMouse = true;
                return;
            }

            m_Brushing = false;
            m_HandlingMiddleMouse = false;

            m_Relaxer.RegisterUndo();

            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            m_BrushCircle.Center = evt.localMousePosition;

            if (!m_Active || m_HandlingMiddleMouse)
                return;

            if (evt.button == (int)MouseButton.MiddleMouse)
            {
                // Redirect to the GraphView.ContentDragger

                m_HandlingMiddleMouse = true;
                return;
            }

            m_BrushingMoving = true;

            evt.StopPropagation();
        }

        // When active, redirect it to the zoomer
        void OnWheel(WheelEvent evt)
        {
            if (!m_Active)
                return;

            // We use this flag to undo the next mouse capture released event
            m_HandlingMiddleMouse = true;

            // GraphView.Zoomer will not work if the mouse is captured
            target.ReleaseMouse();

            m_Zoomer.Invoke(evt);

            target.CaptureMouse();
        }

        // Prevent right-click interaction
        void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (!m_Active && !m_NodeDragger.Active)
                return;

            // UI Toolkit will release the mouse automatically, we use this flag to recapture the mouse when that happens
            m_HandlingContextualMenu = true;

            evt.StopImmediatePropagation();
        }
    }
}

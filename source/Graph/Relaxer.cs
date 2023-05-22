using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphNodeRelax
{
    abstract class Relaxer<TGraph, TNode> where TNode : CacheNode
    {
        // All the individually moveable nodes in the graph
        protected List<TNode> m_Cache;

        // Used to calculate the brush movement delta
        Vector2 m_LastBrushPosition;

#if ALGORITHM_DEBUG
        int debugTarget = 0;
#endif

        // Apply the brush on all moveable nodes
        public virtual void Apply(Brush brush, bool reset, AlgorithmSettings settings)
        {
#if ALGORITHM_DEBUG
            Algorithm.ResetDebug();

            var idx = 0;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (keyboard.digit1Key.isPressed)
                debugTarget = 0;
            if (keyboard.digit2Key.isPressed)
                debugTarget = 1;
            if (keyboard.digit3Key.isPressed)
                debugTarget = 2;
            if (keyboard.digit4Key.isPressed)
                debugTarget = 3;
            if (keyboard.digit5Key.isPressed)
                debugTarget = 4;
            if (keyboard.digit6Key.isPressed)
                debugTarget = 5;
            if (keyboard.digit7Key.isPressed)
                debugTarget = 6;
            if (keyboard.digit8Key.isPressed)
                debugTarget = 7;
            if (keyboard.digit9Key.isPressed)
                debugTarget = 8;
#endif

            if (reset)
            {
                foreach (var node in m_Cache)
                {
                    node.ResetFilter();
                }
            }

            var slideVector = GetBrushMovementDelta(brush, reset);

            foreach (var node in m_Cache)
            {
#if ALGORITHM_DEBUG
                if (idx == debugTarget)
                {
                    DebugRectElement.DebugNode(node);
                }
                idx++;
#endif

                var rect = node.GetPositionInGraph(false);
                var influence = GetBrushInfluence(brush, rect);

                if (Mathf.Approximately(0f, influence))
                    continue;

                var relaxVector = Algorithm.Relax(node.IsStack && !node.TreatStackAsNormalNode, node, settings.Distance, settings.KeepOneToOneEdgesStraight);

                rect.position += relaxVector;
                node.SetPosition(rect, false);

                var collideVector = Algorithm.Collide(m_Cache, node, settings.Distance);

                rect.position += collideVector;
                node.SetPosition(rect, false);
            }

            foreach (var node in m_Cache)
            {
                var rect = node.GetPositionInGraph(false);
                var influence = GetBrushInfluence(brush, rect);

                if (Mathf.Approximately(0f, influence))
                    continue;

                Algorithm.Slide(node, slideVector, influence, settings.SlidePower);
            }

            foreach (var node in m_Cache)
            {
                UpdateNode(node);
            }
        }

        protected abstract void UpdateNode(TNode node);

        public abstract void RegisterUndo();

        Vector2 GetBrushMovementDelta(Brush brush, bool reset)
        {
            if (reset)
                m_LastBrushPosition = brush.Position;

            var slideVector = brush.Position - m_LastBrushPosition;
            m_LastBrushPosition = brush.Position;

            return slideVector;
        }

        static float GetBrushInfluence(Brush brush, Rect element)
        {
            if (Mathf.Approximately(0f, brush.Radius))
                throw new InvalidOperationException("Brush size is 0");

            // Get the closest point along the rect's perimeter if we're not inside it
            var deltaX = brush.Position.x - Mathf.Clamp(brush.Position.x, element.x, element.xMax);
            var deltaY = brush.Position.y - Mathf.Clamp(brush.Position.y, element.y, element.yMax);

            var distSqr = deltaX * deltaX + deltaY * deltaY;

            // Linear falloff
            var influence = 1f - (distSqr / (brush.Radius * brush.Radius));
            influence = Mathf.Clamp01(influence);
            return influence;
        }
    }
}

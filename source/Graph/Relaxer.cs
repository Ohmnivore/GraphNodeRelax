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
                    Algorithm.DebugNode(node);
                }
                idx++;
#endif

                var rect = node.GetPositionInGraph(false);
                var influence = GetBrushInfluence(brush, rect);

                if (Mathf.Approximately(0f, influence))
                    continue;

                var relaxVector = Algorithm.Relax(node.IsStack && !node.TreatStackAsNormalNode, node, influence, settings.RelaxPower, settings.Distance, settings.KeepOneToOneEdgesStraight);
                var (collideVector, numCollisions) = Algorithm.Collide(m_Cache, node, settings.CollisionPower, settings.Distance);

                if (collideVector.magnitude < Mathf.Epsilon)
                    collideVector = Vector2.zero;
                else if (numCollisions == 1)
                {
                    // Prevent relax from moving in the direction that collision is coming from.
                    // (This is only safe to assume when there aren't multiple collisions.)

                    var collideSourceVector = -collideVector;
                    if (Vector2.Dot(relaxVector, collideSourceVector) > 0f)
                    {
                        var relaxRejection = ProjectVector(relaxVector, collideSourceVector);
                        relaxVector -= relaxRejection;
                    }
                }

                collideVector *= influence;

                if (collideVector.magnitude < 0.01f)
                    collideVector = Vector2.zero;

                if (relaxVector.magnitude < 0.01f)
                    relaxVector = Vector2.zero;

#if ALGORITHM_DEBUG
                Debug.Log($"{idx}: relax: {relaxVector.x} {relaxVector.y} | collide: {collideVector.x} {collideVector.y}");
#endif

                var nodePosition = node.GetPositionInGraph(false);
                nodePosition.position += relaxVector + collideVector;
                node.SetPosition(nodePosition, false);
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

        static Vector2 ProjectVector(Vector2 vector, Vector2 onNormal)
        {
            var sqrMagnitude = Vector2.Dot(onNormal, onNormal);
            if (sqrMagnitude < Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            var dot = Vector2.Dot(vector, onNormal);
            return new Vector2(onNormal.x * dot / sqrMagnitude, onNormal.y * dot / sqrMagnitude);
        }
    }
}

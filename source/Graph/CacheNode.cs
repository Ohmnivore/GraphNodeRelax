using System.Collections.Generic;
using UnityEngine;

namespace GraphNodeRelax
{
    /// <summary>
    /// Represents a moveable <see cref="GraphView.Node"/>
    /// </summary>
    public abstract class CacheNode
    {
        /// <summary>
        /// Is this a container of Nodes?
        /// </summary>
        /// <remarks>Not everything that behaves like a stack inherits from <see cref="GraphView.Stack"/>. This cannot be assumed to be a <see cref="GraphView.Stack"/>.</remarks>
        public bool IsStack { get; set; }

        /// <summary>
        /// True when this stack is not connected to any other stacks - so it should be aligned horizontally like a non-stack node
        /// </summary>
        internal bool TreatStackAsNormalNode { get; set; }

        /// <summary>
        /// Width of extra attached elements to the left of this node (ex <see cref="ShaderGraph.PortInputView"/> and <see cref="ShaderGraph.Drawing.Slots"/>)
        /// </summary>
        public float ExtraLeft { get; set; }

        /// <summary>
        /// Width of extra attached elements to the right of this node (ex <see cref="ShaderGraph.PortInputView"/> and <see cref="ShaderGraph.Drawing.Slots"/>)
        /// </summary>
        public float ExtraRight { get; set; }

        /// <summary>
        /// If the node has exactly one input port, this is the port's Y position relative to the node
        /// </summary>
        public float? SingleInputPortY { get; set; }

        /// <summary>
        /// If the node has exactly one output port, this is the port's Y position relative to the node
        /// </summary>
        public float? SingleOutputPortY { get; set; }

        /// <summary>
        /// List of connected input nodes
        /// </summary>
        public List<CacheNode> Inputs { get; } = new List<CacheNode>();

        public List<CacheNode> AllInputs { get; } = new List<CacheNode>();

        /// <summary>
        /// List of connected output nodes
        /// </summary>
        public List<CacheNode> Outputs { get; } = new List<CacheNode>();

        public List<CacheNode> AllOutputs { get; } = new List<CacheNode>();

        /// <summary>
        /// The position and size of the node without any filtering applied.
        /// </summary>
        /// <remarks>As a bonus, we handle floating point coordinate increments this way (UI Toolkit rounds the layouts).</remarks>
        Rect? m_UnfilteredRect;

        /// <summary>
        /// The relax algos run instantly, which would be jarring to see.
        /// This filtering is done to make it look pleasant.
        /// </summary>
        EMAFilter m_Filter = new EMAFilter(30);

        /// <summary>
        /// To be able to tell if the node is moving on the screen (from user input or residual filtering).
        /// </summary>
        Vector2 m_LastFilteredPosition;

        /// <summary>
        /// Get the unfiltered node position relative to its parent.
        /// </summary>
        /// <param name="includeExtra">Includes <see cref="ExtraLeft"/> and <see cref="ExtraRight"> in the rect bounds.</param>
        /// <returns>A rectangle with position and dimensions.</returns>
        internal Rect GetPosition(bool includeExtra)
        {
            var rect = GetNodePosition();

            if (!m_UnfilteredRect.HasValue)
                m_UnfilteredRect = rect;
            else
                rect = m_UnfilteredRect.Value;

            if (includeExtra)
            {
                rect.xMin += ExtraLeft;
                rect.xMax += ExtraRight;
            }

            return rect;
        }

        /// <summary>
        /// Set the unfiltered node position relative to its parent.
        /// </summary>
        /// <param name="rect">A rectangle with position and dimensions.</param>
        /// <param name="includeExtra">Includes <see cref="ExtraLeft"/> and <see cref="ExtraRight"> in the rect bounds.</param>
        internal void SetPosition(Rect rect, bool includeExtra)
        {
            if (includeExtra)
            {
                rect.xMin -= ExtraLeft;
                rect.xMax -= ExtraRight;
            }

            m_UnfilteredRect = rect;
        }

        /// <summary>
        /// Apply the filtered position to the actual node.
        /// </summary>
        /// <returns>True if the node was moved.</returns>
        internal bool UpdatePosition()
        {
            if (m_UnfilteredRect is { } rect)
            {
                // What we actually want to see in the GraphView at all times is the filtered position
                m_Filter.Add(rect.position);
                var filteredPosition = m_Filter.Average;
                rect.position = filteredPosition;

                SetNodePosition(rect);

                var positionChanged = filteredPosition != m_LastFilteredPosition;
                m_LastFilteredPosition = filteredPosition;

                return positionChanged;
            }

            return false;
        }

        /// <summary>
        /// Should be called in between brush sessions to clear any residual filtering data.
        /// </summary>
        internal void ResetFilter()
        {
            m_UnfilteredRect = null;
            m_LastFilteredPosition = Vector2.zero;
            m_Filter.Reset(GetNodePosition().position);
        }

        /// <summary>
        /// Implementation-specific node position getter for <see cref="GetPosition(bool)"/>.
        /// </summary>
        /// <returns>A rectangle with position and dimensions.</returns>
        protected abstract Rect GetNodePosition();

        /// <summary>
        /// Implementation-specific node position setter for <see cref="SetPosition(Rect, bool)"/>.
        /// </summary>
        /// <param name="position">A rectangle with position and dimensions.</param>
        protected abstract void SetNodePosition(Rect position);

        /// <summary>
        /// Get the unfiltered node position relative to the graph's content container.
        /// </summary>
        /// <param name="includeExtra">Includes <see cref="ExtraLeft"/> and <see cref="ExtraRight"> in the rect bounds.</param>
        /// <returns>A rectangle with position and dimensions.</returns>
        public abstract Rect GetPositionInGraph(bool includeExtra);
    }
}

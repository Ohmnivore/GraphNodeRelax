using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    /// <summary>
    /// Represents a moveable <see cref="GraphView.Node"/>
    /// </summary>
    public class GraphViewCacheNode : CacheNode
    {
        /// <summary>
        /// The <see cref="GraphView.Node"/> that this instance represents
        /// </summary>
        public Node Node { get; internal set; }

        /// <summary>
        /// The <see cref="GraphView.Scope"/> (Group) that this Node belongs to, if any.
        /// Nodes from different scopes shouldn't interact with each other.
        /// </summary>
        public Scope Scope { get; internal set; }

        /// <summary>
        /// The <see cref="GraphView"/> that this node belongs to
        /// </summary>
        public GraphView GraphView { get; internal set; }

        /// <inheritdoc/>
        protected override Rect GetNodePosition()
        {
            return Node.GetPosition();
        }

        /// <inheritdoc/>
        protected override void SetNodePosition(Rect position)
        {
            Node.SetPosition(position);
        }

        /// <inheritdoc/>
        public override Rect GetPositionInGraph(bool includeExtra)
        {
            var rect = GetPosition(includeExtra);
            var rectInGraphView = Node.parent.ChangeCoordinatesTo(GraphView.contentViewContainer, rect);
            return rectInGraphView;
        }
    }
}

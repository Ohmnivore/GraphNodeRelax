using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace GraphNodeRelax
{
    /// <summary>
    /// Prepares nodes to be stored in a cached representation of the graph.
    /// </summary>
    /// <remarks>
    /// A loose structure (neighbor resolution, bounds resolution, and post-processing) is provided but not enforced.
    /// By the end of the process the builder should have set all the node's publicly settable fields in <see cref="CacheNode"/> and <see cref="GraphViewCacheNode"/>.
    /// </remarks>
    /// <seealso cref="DefaultGraphViewBuilder"/>
    /// <seealso cref="DefaultGraphViewBuilderImpl"/>
    public interface IGraphViewBuilder
    {
        /// <summary>
        /// Setup the builder.
        /// </summary>
        /// <param name="graphView">The graph that the nodes belong to</param>
        void Setup(GraphView graphView);

        /// <summary>
        /// Returns the stack that this node belongs to, if any.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <returns>The stack or null if none</returns>
        Node GetStack(Node node);

        /// <summary>
        /// Finds and stores connections between the specified node and other <see cref="GraphViewCacheNode"/>s in the graph.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <param name="nodeToCacheNode">Map of known <see cref="Node"/>s to their cached representations</param>
        void FindNeighbors(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode);

        /// <summary>
        /// Compute the bounds of the specified node, including any attachments such as inline parameters.
        /// </summary>
        /// <param name="node">The specified node</param>
        void ComputeFullBounds(GraphViewCacheNode node);

        /// <summary>
        /// Process the specified node after <see cref="FindNeighbors(GraphViewCacheNode, Dictionary{Node, GraphViewCacheNode})"/>
        /// and <see cref="ComputeFullBounds(GraphViewCacheNode)"/> have been called on all nodes.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <param name="nodeToCacheNode">Map of known <see cref="Node"/>s to their cached representations</param>
        void PostProcess(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode);
    }
}

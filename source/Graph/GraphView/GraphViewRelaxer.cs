using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace GraphNodeRelax
{
    class GraphViewRelaxer : Relaxer<GraphView, GraphViewCacheNode>
    {
        // The target graph
        GraphView m_GraphView;

        // Prevents nodes from registering undo on every frame - should only register on the first changed frame
        HashSet<GraphElement> m_HasRegisteredUndo = new HashSet<GraphElement>();

        // GraphView's version of Undo.RegisterCompleteObjectUndo
        GraphViewChange m_GraphViewChange = new GraphViewChange { movedElements = new List<GraphElement>() };

        // Processes graph data into a cache for fast brush operations.
        // Assumes that the graph does not change during a brush session.
        public void Setup(GraphView graphView, IGraphViewBuilder builder)
        {
            m_GraphView = graphView;
            var nodes = graphView.nodes.ToList();

            builder.Setup(graphView);

            var nodeToCachedNode = new Dictionary<Node, GraphViewCacheNode>();
            m_Cache = new List<GraphViewCacheNode>(nodes.Count);

            foreach (var node in nodes)
            {
                if (nodeToCachedNode.ContainsKey(node))
                    continue;

                if (builder.GetStack(node) is { } stackLikeNode)
                {
                    // This node belongs to another node, so it's not moveable.
                    // Its linked nodes should consider it as its containing stack node instead.

                    if (nodeToCachedNode.TryGetValue(stackLikeNode, out var cacheStackLikeNode))
                    {
                        cacheStackLikeNode.IsStack = true;
                        nodeToCachedNode.Add(node, cacheStackLikeNode);
                    }
                    else
                    {
                        var newCacheStackLikeNode = new GraphViewCacheNode
                        {
                            Node = stackLikeNode,
                            GraphView = graphView,
                            IsStack = true
                        };

                        nodeToCachedNode.Add(node, newCacheStackLikeNode);
                        m_Cache.Add(newCacheStackLikeNode);
                    }
                }
                else
                {
                    // This node is a freely moveable node

                    var cacheNode = new GraphViewCacheNode
                    {
                        Node = node,
                        GraphView = graphView,
                        IsStack = false
                    };

                    nodeToCachedNode.Add(node, cacheNode);
                    m_Cache.Add(cacheNode);
                }
            }

            // At this point we still don't know which nodes are stacks
            foreach (var cacheNode in m_Cache)
            {
                builder.FindNeighbors(cacheNode, nodeToCachedNode);
                builder.ComputeFullBounds(cacheNode);
                builder.PostProcess(cacheNode, nodeToCachedNode);
            }

            // At this point we know which nodes are stacks
            foreach (var cacheNode in m_Cache)
            {
                if (cacheNode.IsStack)
                {
                    // Is this node technically a stack but not connected to any stacks?
                    cacheNode.TreatStackAsNormalNode = !cacheNode.Inputs.Any(x => x.IsStack) && !cacheNode.Outputs.Any(x => x.IsStack);

                    if (!cacheNode.TreatStackAsNormalNode)
                    {
                        // Stack nodes should only be aligned relative to other stack nodes
                        cacheNode.Inputs.RemoveAll(x => !x.IsStack);
                        cacheNode.Outputs.RemoveAll(x => !x.IsStack);
                    }
                }
            }
        }

        public override void Apply(Brush brush, bool reset, AlgorithmSettings settings)
        {
            if (reset)
            {
                m_HasRegisteredUndo.Clear();
                m_GraphViewChange.movedElements.Clear();
            }

            base.Apply(brush, reset, settings);

            if (m_GraphViewChange.movedElements.Count > 0)
            {
                m_GraphView.graphViewChanged?.Invoke(m_GraphViewChange);
                m_GraphViewChange.movedElements.Clear();
            }
        }

        protected override void UpdateNode(GraphViewCacheNode node)
        {
            var hasMoved = node.UpdatePosition();

            if (hasMoved && !m_HasRegisteredUndo.Contains(node.Node))
            {
                m_HasRegisteredUndo.Add(node.Node);
                m_GraphViewChange.movedElements.Add(node.Node);
            }
        }

        public override void RegisterUndo()
        {
            m_GraphViewChange.movedElements.Clear();

            foreach (var node in m_HasRegisteredUndo)
            {
                m_GraphViewChange.movedElements.Add(node);
            }

            m_HasRegisteredUndo.Clear();

            if (m_GraphViewChange.movedElements.Count > 0)
            {
                m_GraphView.graphViewChanged?.Invoke(m_GraphViewChange);
                m_GraphViewChange.movedElements.Clear();
            }
        }
    }
}

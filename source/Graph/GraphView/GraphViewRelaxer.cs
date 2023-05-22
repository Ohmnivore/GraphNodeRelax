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
                cacheNode.AllInputs.Capacity = cacheNode.Inputs.Count;
                foreach (var input in cacheNode.Inputs)
                    cacheNode.AllInputs.Add(input);

                cacheNode.AllOutputs.Capacity = cacheNode.Outputs.Count;
                foreach (var output in cacheNode.Outputs)
                    cacheNode.AllOutputs.Add(output);

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

            // Order by outside-in, starting by the leaves.
            // This ensures optimal relax solution: the most independant nodes are solved first.
            var orderedCache = new List<GraphViewCacheNode>();
            foreach (var cacheNode in m_Cache)
            {
                var noInputs = cacheNode.AllInputs.Count == 0;
                var noOutputs = cacheNode.AllOutputs.Count == 0;

                if (noInputs != noOutputs ||
                    (noInputs && noOutputs))
                {
                    orderedCache.Add(cacheNode);
                }
            }
            var lastFilled = 0;
            while (orderedCache.Count != m_Cache.Count)
            {
                var filled = orderedCache.Count;

                for (int i = lastFilled; i < filled; i++)
                {
                    var cacheNode = orderedCache[i];

                    foreach (GraphViewCacheNode input in cacheNode.AllInputs)
                    {
                        orderedCache.AddUnique(input);
                    }
                    foreach (GraphViewCacheNode output in cacheNode.AllOutputs)
                    {
                        orderedCache.AddUnique(output);
                    }
                }

                lastFilled += filled;
            }
            m_Cache = orderedCache;
        }

        public override void Apply(Brush brush, bool reset, AlgorithmSettings settings)
        {
#if ALGORITHM_DEBUG
            DebugRectElement.DebugParent(m_GraphView.contentViewContainer);
#endif

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

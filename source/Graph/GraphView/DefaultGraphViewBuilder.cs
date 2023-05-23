using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    /// <summary>
    /// <para>
    /// This default <see cref="IGraphViewBuilder"/> implementation works for ShaderGraph and VFXGraph.
    /// It makes assumptions that may or may not hold true for other graphs.
    /// </para>
    /// 
    /// <para>
    /// If necessary other <see cref="IGraphViewBuilder"/> implementations (or classes deriving from
    /// <see cref="DefaultGraphViewBuilder"/>) can be written for specific graphs.
    /// </para>
    /// </summary>
    /// <seealso cref="DefaultGraphViewBuilderImpl"/>
    [GraphViewBuilder(typeof(GraphView))]
    public class DefaultGraphViewBuilder : IGraphViewBuilder
    {
        /// <summary>
        /// The target graph view
        /// </summary>
        protected GraphView m_GraphView;

        /// <summary>
        /// The port connections in the target graph
        /// </summary>
        protected EdgeCache m_EdgeCache = new EdgeCache();

        /// <inheritdoc/>
        public virtual void Setup(GraphView graphView)
        {
            m_GraphView = graphView;
            m_EdgeCache.Setup(m_GraphView);
        }

        /// <inheritdoc/>
        public virtual Node GetStack(Node node)
        {
            return DefaultGraphViewBuilderImpl.GetStack(node);
        }

        /// <inheritdoc/>
        public virtual void FindNeighbors(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode)
        {
            DefaultGraphViewBuilderImpl.FindNeighbors(node, nodeToCacheNode);
        }

        /// <inheritdoc/>
        public virtual void ComputeFullBounds(GraphViewCacheNode node)
        {
            DefaultGraphViewBuilderImpl.ComputeFullBounds(node);
        }

        /// <inheritdoc/>
        public virtual void PostProcess(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode)
        {
            DefaultGraphViewBuilderImpl.ProcessPorts(node, nodeToCacheNode, m_EdgeCache);
        }
    }

    /// <summary>
    /// Logic used by <see cref="DefaultGraphViewBuilder"/>.
    /// </summary>
    public static class DefaultGraphViewBuilderImpl
    {
        /// <summary>
        /// Resolves the specified node's parent stack node through the UITK hierarchy.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <returns>The parent stack node, or null if none</returns>
        public static Node GetStack(Node node)
        {
            return node.GetFirstAncestorOfType<Node>();
        }

        /// <summary>
        /// Finds and stores connections between the specified node and other <see cref="GraphViewCacheNode"/>s in the graph.
        /// Uses <see cref="Node.CollectElements(HashSet{GraphElement}, Func{GraphElement, bool})"/>.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <param name="nodeToCacheNode">Map of known <see cref="Node"/>s to their cached representations</param>
        public static void FindNeighbors(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode)
        {
            var siblings = new HashSet<GraphElement>();
            node.Node.CollectElements(siblings, x => true);

            foreach (var element in siblings)
            {
                if (element is Edge edge && TryGetNodeConnectedByEdge(edge, node.Node, out var otherNode, out var edgeDirection))
                {
                    if (nodeToCacheNode.TryGetValue(otherNode, out var otherCacheNode))
                    {
                        if (node.Scope != otherCacheNode.Scope)
                            continue;

                        if (edgeDirection == Direction.Input)
                        {
                            node.Inputs.AddUnique(otherCacheNode);
                            otherCacheNode.Outputs.AddUnique(node);
                        }
                        else
                        {
                            node.Outputs.AddUnique(otherCacheNode);
                            otherCacheNode.Inputs.AddUnique(node);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("No cached representation of this neighboring node in nodeToCacheNode");
                    }
                }
            }
        }

        /// <summary>
        /// Compute the bounds of the specified node, including any attachments such as inline parameters.
        /// Accounts for extra node attachments like ShaderGraph.PortInputView and ShaderGraph.Drawing.Slots.
        /// </summary>
        /// <param name="node">The specified node</param>
        public static void ComputeFullBounds(GraphViewCacheNode node)
        {
            var nodeRect = node.Node.GetPosition();

            foreach (var x in node.Node.Query<GraphElement>().ToList())
            {
                if (x.resolvedStyle.display == DisplayStyle.None)
                    continue;

                if (!x.capabilities.HasFlag(Capabilities.Movable) ||
                    x.pickingMode == PickingMode.Ignore)
                {
                    // Try to resolve the container element: ShaderGraph.PortInputView does not use VisualElement.contentContainer for some reason
                    var container = x.Q<VisualElement>("container");
                    container ??= x.contentContainer;

                    var rect = container.parent.ChangeCoordinatesTo(node.Node, container.layout);
                    node.ExtraLeft = Mathf.Min(node.ExtraLeft, rect.xMin);
                    node.ExtraRight = Mathf.Max(node.ExtraRight, rect.xMax - nodeRect.width);
                }
            }
        }

        /// <summary>
        /// Detects stacks (based on the presence of vertical ports) and edges between them.
        /// Also detects single input/output ports.
        /// </summary>
        /// <param name="node">The specified node</param>
        /// <param name="nodeToCacheNode">Map of known <see cref="Node"/>s to their cached representations</param>
        public static void ProcessPorts(GraphViewCacheNode node, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode, IEdgeCache edgeCache)
        {
            var nodeRect = node.Node.GetPosition();
            var nodeCenter = new Rect(0f, 0f, nodeRect.width, nodeRect.height).center;
            var allPorts = node.Node.Query<Port>().ToList();

            var inputPortY = 0f;
            var numInputPorts = 0;
            var outputPortY = 0f;
            var numOutputsPorts = 0;

            Port topPort = null;
            var minY = float.MaxValue;

            Port bottomPort = null;
            var maxY = float.MinValue;

            foreach (var port in allPorts)
            {
                // Skip hidden ports
                if (Mathf.Approximately(0f, port.layout.width) && Mathf.Approximately(0f, port.layout.height))
                    continue;

                var portGlobalCenter = port.GetGlobalCenter();
                var portCenter = node.Node.WorldToLocal(new Vector2(portGlobalCenter.x, portGlobalCenter.y));

                // Vertical ports are horizontally centered
                if (Mathf.Abs(portCenter.x - nodeCenter.x) < 8f)
                {
                    if (portCenter.y < minY && portCenter.y < nodeCenter.y)
                    {
                        topPort = port;
                        minY = portCenter.y;
                    }

                    if (portCenter.y > maxY && portCenter.y > nodeCenter.y)
                    {
                        bottomPort = port;
                        maxY = portCenter.y;
                    }
                }
                else if (port.connected && portCenter.x < nodeCenter.x)
                {
                    inputPortY = portCenter.y;
                    numInputPorts++;
                }
                else if (port.connected && portCenter.x > nodeCenter.x)
                {
                    outputPortY = portCenter.y;
                    numOutputsPorts++;
                }
            }

            // Stack detection based on presence of vertical ports
            if (topPort != null || bottomPort != null)
            {
                node.IsStack = true;
            }

            if (topPort != null)
            {
                ProcessStackPort(node, topPort, node.Inputs, nodeToCacheNode, edgeCache);
            }

            if (bottomPort != null)
            {
                ProcessStackPort(node, bottomPort, node.Outputs, nodeToCacheNode, edgeCache);
            }

            if (numInputPorts == 1)
            {
                node.SingleInputPortY = inputPortY;
            }

            if (numOutputsPorts == 1)
            {
                node.SingleOutputPortY = outputPortY;
            }
        }

        // Resolves the connection for a stack port (using informal connections when necessary)
        static void ProcessStackPort(GraphViewCacheNode node, Port port, IList<CacheNode> addTo, Dictionary<Node, GraphViewCacheNode> nodeToCacheNode, IEdgeCache edgeCache)
        {
            if (port.connected) // Formal connection
            {
                foreach (var edge in port.connections)
                {
                    if (TryGetNodeConnectedByEdge(edge, node.Node, out var otherNode, out var _))
                    {
                        if (nodeToCacheNode.TryGetValue(otherNode, out var otherCacheNode))
                        {
                            if (node.Scope != otherCacheNode.Scope)
                                continue;

                            addTo.AddUnique(otherCacheNode);
                        }
                    }
                }
            }
            else if (edgeCache.TryGetConnections(port, out var connections)) // Informal connection
            {
                foreach (var connection in connections)
                {
                    if (nodeToCacheNode.TryGetValue(connection.node, out var otherCacheNode))
                    {
                        if (node.Scope != otherCacheNode.Scope)
                            continue;

                        addTo.AddUnique(otherCacheNode);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the opposite node of a specified edge (and its direction).
        /// </summary>
        /// <param name="edge">The specified ege</param>
        /// <param name="firstNode">The "origin" node</param>
        /// <param name="otherNode">The node on the other end of <paramref name="firstNode"/></param>
        /// <param name="edgeDirection">The direction of <paramref name="otherNode"/> relative to <paramref name="firstNode"/></param>
        /// <returns>True if there's a connected node</returns>
        public static bool TryGetNodeConnectedByEdge(Edge edge, Node firstNode, out Node otherNode, out Direction edgeDirection)
        {
            if (edge.input is { } inputPort &&
                inputPort.node == firstNode)
            {
                if (edge.output is { } port)
                {
                    otherNode = port.node;
                    edgeDirection = Direction.Input;
                    return true;
                }
            }

            if (edge.output is { } outputPort &&
                outputPort.node == firstNode)
            {
                if (edge.input is { } port)
                {
                    otherNode = port.node;
                    edgeDirection = Direction.Output;
                    return true;
                }
            }

            otherNode = default;
            edgeDirection = default;
            return false;
        }
    }
}

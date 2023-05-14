using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    /// <summary>
    /// Provides port-port connection queries.
    /// </summary>
    public interface IEdgeCache
    {
        /// <summary>
        /// Gets all the ports connected to the specified port.
        /// </summary>
        /// <param name="port">The specified port.</param>
        /// <param name="connections">The connections list to populate.</param>
        /// <returns>True if any connections were found.</returns>
        bool TryGetConnections(Port port, out IEnumerable<Port> connections);
    }

    /// <summary>
    /// Some edges are not formally assigned to their ports (ex between <see cref="ShaderGraph.ContextView"/> pairs),
    /// we can only detect those connections by querying the UI tree.
    /// </summary>
    public class EdgeCache : IEdgeCache
    {
        readonly Dictionary<Port, List<Port>> m_ConnectionCache = new Dictionary<Port, List<Port>>();

        /// <inheritdoc/>
        public bool TryGetConnections(Port port, out IEnumerable<Port> connections)
        {
            if (m_ConnectionCache.TryGetValue(port, out var c))
            {
                connections = c;
                return true;
            }

            connections = default;
            return false;
        }

        public void Setup(GraphView graphView)
        {
            m_ConnectionCache.Clear();

            var allEdges = graphView.Query<Edge>().ToList();

            foreach (var edge in allEdges)
            {
                if (edge.input != null && edge.output != null)
                {
                    if (m_ConnectionCache.TryGetValue(edge.input, out var inputConnections))
                    {
                        inputConnections.Add(edge.output);
                    }
                    else
                    {
                        m_ConnectionCache.Add(edge.input, new List<Port>(new[] { edge.output }));
                    }

                    if (m_ConnectionCache.TryGetValue(edge.output, out var outputConnections))
                    {
                        outputConnections.Add(edge.output);
                    }
                    else
                    {
                        m_ConnectionCache.Add(edge.output, new List<Port>(new[] { edge.input }));
                    }
                }
            }
        }
    }
}

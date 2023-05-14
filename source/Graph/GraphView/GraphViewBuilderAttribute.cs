using System;

namespace GraphNodeRelax
{
    /// <summary>
    /// An attribute placed on a class inheriting from <see cref="IGraphViewBuilder"/> to associate it
    /// with a specific <see cref="GraphView"/> implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GraphViewBuilderAttribute : Attribute
    {
        /// <summary>
        /// The type of the <see cref="GraphView"/>.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// When many attributes are used for the same <see cref="Type"/>, the attribute with the higher priority will be used.
        /// </summary>
        public int Priority { get; set; } = 0;

        public GraphViewBuilderAttribute(Type type)
        {
            Type = type;
        }
    }
}

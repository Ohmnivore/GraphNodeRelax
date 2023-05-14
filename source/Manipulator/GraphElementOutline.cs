using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    class GraphElementOutline : VisualElement
    {
        public GraphElementOutline()
        {
            style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = style.borderBottomWidth = 1f;
            style.borderTopLeftRadius = style.borderTopRightRadius = style.borderBottomLeftRadius = style.borderBottomRightRadius = 8f;

            style.position = Position.Absolute;

            pickingMode = PickingMode.Ignore;

            Hide();
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
        }

        public void Update(GraphView graphView, GraphElement target, Color color)
        {
            if (graphView == null || target == null)
            {
                Hide();
                return;
            }

            style.display = DisplayStyle.Flex;

            style.borderLeftColor = style.borderRightColor = style.borderTopColor = style.borderBottomColor = color;

            var newLayout = target.parent.ChangeCoordinatesTo(graphView, target.GetPosition());

            style.left = newLayout.x;
            style.top = newLayout.y;
            style.width = newLayout.width;
            style.height = newLayout.height;
        }
    }
}

#if ALGORITHM_DEBUG
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    class DebugRectElement : VisualElement
    {
        static VisualElement parentElement;
        static CacheNode debugNode;

        public static void DebugParent(VisualElement parent)
        {
            parentElement = parent;
        }

        public static void DebugNode(CacheNode node)
        {
            debugNode = node;
        }

        public DebugRectElement(Color color, bool vertical = true)
        {
            style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = style.borderBottomWidth = 1f;
            style.borderLeftColor = style.borderRightColor = style.borderTopColor = style.borderBottomColor = color;
            style.position = Position.Absolute;

            pickingMode = PickingMode.Ignore;

            parentElement.Add(this);
        }

        public void Reset()
        {
            style.top = 0f;
            style.left = 0f;
            style.width = 0f;
            style.height = 0f;
        }

        public void SetRect(Rect rect, CacheNode node)
        {
            if (node != debugNode)
                return;

            style.top = rect.y;
            style.left = rect.x;
            style.width = rect.width;
            style.height = rect.height;
        }
    }
}
#endif

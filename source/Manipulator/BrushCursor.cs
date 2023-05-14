using UnityEngine.UIElements;
using UnityEngine;

namespace GraphNodeRelax
{
    // Hides the mouse cursor when the brush is visible.
    // It's not perfect: UI Toolkit will use the cursor of the topmost element when a mouse capture is started.
    // And the manipulator target will not always be the topmost element.
    class BrushCursor
    {
        UnityEngine.UIElements.Cursor m_BrushCursor;
        StyleCursor m_OriginalCursor;

        public BrushCursor(VisualElement target)
        {
            // Create a fully transparent invisible cursor
            var brushCursorTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (var x = 0; x < brushCursorTexture.width; x++)
            {
                for (var y = 0; y < brushCursorTexture.height; y++)
                {
                    brushCursorTexture.SetPixel(x, y, Color.clear);
                }
            }

            brushCursorTexture.Apply();
            m_BrushCursor = new UnityEngine.UIElements.Cursor();
            m_BrushCursor.texture = brushCursorTexture;

            m_OriginalCursor = target.style.cursor;
        }

        public void Update(VisualElement target, bool active)
        {
            target.style.cursor = active
                ? m_BrushCursor
                : m_OriginalCursor;
        }

        public void RestoreCursor(VisualElement target)
        {
            Update(target, false);
        }

        public void ApplyCursor(VisualElement target)
        {
            Update(target, true);
        }
    }
}

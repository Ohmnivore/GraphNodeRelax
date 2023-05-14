using UnityEngine.UIElements;
using UnityEngine;

namespace GraphNodeRelax
{
    // Handles user input that resizes the brush radius
    class BrushResizer
    {
        bool m_IncreasePressed;
        bool m_DecreasePressed;

        // An ergonomically pleasing resize function: slower when the radius is small and faster when it's large
        public static float NewRadius(float currentRadius, float speed, float min, float max)
        {
            var factor = 0.05f + (currentRadius - min) / (max - min) * 0.95f;
            factor = Mathf.Pow(factor, 0.5f);

            var newRadius = currentRadius + speed * factor;

            return Mathf.Clamp(newRadius, min, max);
        }

        public void Update(VisualElement target, Settings settings)
        {
            var brush = settings.Brush;

            if (m_IncreasePressed && !m_DecreasePressed)
            {
                var radius = NewRadius(brush.Radius, brush.RadiusResizeSpeed * EditorTime.DeltaTime, brush.MinimumRadius, GetMaxRadius(target));
                Settings.instance.SetBrushRadius(radius);
            }
            else if (m_DecreasePressed && !m_IncreasePressed)
            {
                var radius = NewRadius(brush.Radius, -brush.RadiusResizeSpeed * EditorTime.DeltaTime, brush.MinimumRadius, GetMaxRadius(target));
                Settings.instance.SetBrushRadius(radius);
            }
        }

        public void OnKeyDown(KeyDownEvent evt, KeyboardShortcutSettings settings)
        {
            if (settings.IncreaseBrushRadius.Matches(evt))
            {
                m_IncreasePressed = true;
            }
            else if (settings.DecreaseBrushRadius.Matches(evt))
            {
                m_DecreasePressed = true;
            }
        }

        public void OnKeyUp(KeyUpEvent evt, KeyboardShortcutSettings settings)
        {
            if (settings.IncreaseBrushRadius.WasReleased(evt))
            {
                m_IncreasePressed = false;
            }

            if (settings.DecreaseBrushRadius.WasReleased(evt))
            {
                m_DecreasePressed = false;
            }
        }

        public void Reset()
        {
            m_IncreasePressed = false;
            m_DecreasePressed = false;
        }

        // We limit the brush diameter to the size of the window
        float GetMaxRadius(VisualElement target)
        {
            return Mathf.Min(target.resolvedStyle.width, target.resolvedStyle.height) * 0.5f;
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace GraphNodeRelax
{
    [Serializable]
    class BrushSettings
    {
        [Tooltip("The effective radius of the brush in Unity window pixels.")]
        public float Radius = 80f;

        [Tooltip("The minimum allowed radius of the brush.")]
        [Min(5f)]
        public float MinimumRadius = 16f;

        [Tooltip("The speed of the brush resize operation.")]
        [Min(5f)]
        public float RadiusResizeSpeed = 400f;

        [Tooltip("The color of the brush when it's not pressed.")]
        public Color InactiveColor = Color.white;

        [Tooltip("The color of the brush when it's pressed.")]
        public Color ActiveColor = Color.green;
    }

    [Serializable]
    class AlgorithmSettings
    {
        [Tooltip("For nodes that share a single edge, aligns them by port rather than by midpoint. This makes the edge between them a 90 degree line.")]
        public bool KeepOneToOneEdgesStraight = true;

        [Tooltip("Desired distance between nodes.")]
        [Min(5f)]
        public float Distance = 40f;

        [Tooltip("The strength of the force that pushes nodes together and apart.")]
        [Range(0f, 1f)]
        public float RelaxPower = 0.1f;

        [Tooltip("The strength of the force that pushes nodes in the direction of brush movement.")]
        [Range(0f, 1f)]
        public float SlidePower = 0.6f;

        [Tooltip("The strength of the force that pushes overlapping nodes apart.")]
        [Range(0f, 1f)]
        public float CollisionPower = 0.9f;
    }

    [Serializable]
    class KeyboardShortcutSettings
    {
        [Tooltip("Enable the brush inside a graph editor window when pressed.")]
        public KeyboardShortcut EnableBrush = new KeyboardShortcut(KeyCode.R, EventModifiers.Shift);

        [Tooltip("Disables the brush inside a graph editor window when pressed.")]
        public KeyboardShortcut DisableBrush = new KeyboardShortcut(KeyCode.Escape);

        [Tooltip("Enables the single node drag mode while held.")]
        public KeyboardShortcut EnableSingleNodeDragMode = new KeyboardShortcut(KeyCode.LeftShift, EventModifiers.Shift);

        [Tooltip("Increases the brush radius while held.")]
        public KeyboardShortcut IncreaseBrushRadius = new KeyboardShortcut(KeyCode.Equals);

        [Tooltip("Decreases the brush radius while held.")]
        public KeyboardShortcut DecreaseBrushRadius = new KeyboardShortcut(KeyCode.Minus);
    }

    [FilePath(FilePath, FilePathAttribute.Location.PreferencesFolder)]
    class Settings : ScriptableSingleton<Settings>
    {
        const string FilePath = "GraphNodeRelax/GraphNodeRelax.settings";

        public BrushSettings Brush => m_Brush;

        public AlgorithmSettings Algorithm => m_Algorithm;

        public KeyboardShortcutSettings Shortcuts => m_Shortcuts;

        [SerializeField]
        BrushSettings m_Brush = new BrushSettings();

        [SerializeField]
        AlgorithmSettings m_Algorithm = new AlgorithmSettings();

        [SerializeField]
        KeyboardShortcutSettings m_Shortcuts = new KeyboardShortcutSettings();

        public void SetBrushRadius(float radius)
        {
            if (radius != m_Brush.Radius)
            {
                m_Brush.Radius = radius;
                SaveChanges();
            }
        }

        public void SaveChanges()
        {
            Save(true);
        }

        public void Reset()
        {
            m_Brush = new BrushSettings();
            m_Algorithm = new AlgorithmSettings();
            m_Shortcuts = new KeyboardShortcutSettings();

            SaveChanges();
        }
    }
}

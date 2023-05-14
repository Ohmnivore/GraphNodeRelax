using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    // Basic keyboard key + modifiers shortcut that supports the legacy and the new input systems
    [Serializable]
    struct KeyboardShortcut
    {
        public KeyCode Key;
        public EventModifiers Modifiers;

        public KeyboardShortcut(KeyCode key, EventModifiers modifiers = EventModifiers.None)
        {
            Key = key;
            Modifiers = modifiers;
        }

        // Are the key and the exact same modifiers (not more, not less) pressed?
        public bool Matches(KeyDownEvent evt)
        {
            return evt.keyCode == Key && evt.modifiers == Modifiers;
        }

        // Was the key or any of its modifiers released?
        public bool WasReleased(KeyUpEvent evt)
        {
            var modifierReleased =
                Modifiers != EventModifiers.None &&
                ((int)evt.modifiers & (int)Modifiers) == 0;

            return evt.keyCode == Key || modifierReleased;
        }
    }
}

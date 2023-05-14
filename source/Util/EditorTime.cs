using UnityEditor;
using UnityEngine;

namespace GraphNodeRelax
{
    // Time.deltaTime doesn't work in the editor. This is a substitute.
    static class EditorTime
    {
        public static float DeltaTime => s_DeltaTime;

        static double s_LastTimestamp = -1.0;
        static float s_DeltaTime;

        static EditorTime()
        {
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            var newTimestamp = Time.realtimeSinceStartupAsDouble;

            if (s_LastTimestamp < 0)
                s_DeltaTime = 0f;
            else
                s_DeltaTime = (float)(newTimestamp - s_LastTimestamp);

            s_LastTimestamp = newTimestamp;
        }
    }
}

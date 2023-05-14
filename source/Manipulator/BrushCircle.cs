using UnityEngine;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    // Based on UnityEditor.Experimental.GraphView.RectangleSelect
    class BrushCircle : ImmediateModeElement
    {
        const float s_SegmentLength = 5f; // Matches RectangleSelect segment length
        static Material s_LineMaterial;

        // https://docs.unity3d.com/2023.2/Documentation/ScriptReference/GL.html
        static Material GetLineMaterial()
        {
            if (s_LineMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                s_LineMaterial = new Material(shader);
                s_LineMaterial.hideFlags = HideFlags.HideAndDontSave;
                s_LineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_LineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_LineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                s_LineMaterial.SetInt("_ZWrite", 0);
            }

            return s_LineMaterial;
        }

        Vector2 m_Center;
        float m_Radius;
        Color m_Color;

        public Vector2 Center
        {
            get => m_Center;
            set
            {
                if (value != m_Center)
                    MarkDirtyRepaint();
                m_Center = value;
            }
        }

        public float Radius
        {
            get => m_Radius;
            set
            {
                if (value != m_Radius)
                    MarkDirtyRepaint();
                m_Radius = value;
            }
        }

        public Color Color
        {
            get => m_Color;
            set
            {
                if (value != m_Color)
                    MarkDirtyRepaint();
                m_Color = value;
            }
        }

        float Circumference => 2f * Mathf.PI * Radius;

        protected override void ImmediateRepaint()
        {
            if (Radius == 0f)
                return;

            var screenCenter = Center + parent.layout.position;

            DrawCircle(screenCenter, Radius, s_SegmentLength, Color);
        }

        private void DrawCircle(Vector2 center, float radius, float segmentsLength, Color col)
        {
            var numLines = Mathf.FloorToInt(Circumference / segmentsLength);

            // Avoid having a filled line before and after 0 degrees
            if (numLines % 2 == 1)
                numLines += 1;

            GetLineMaterial().SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(col);

            // The i += 2 creates a dotted pattern
            for (var i = 0; i <= numLines; i += 2)
            {
                var progress1 = i / (float)numLines;
                var progress2 = (i + 1) / (float)numLines;

                GL.Vertex(new Vector3(
                    center.x + radius * Mathf.Cos(progress1 * 2.0f * Mathf.PI),
                    center.y + radius * Mathf.Sin(progress1 * 2.0f * Mathf.PI)
                ));

                GL.Vertex(new Vector3(
                    center.x + radius * Mathf.Cos(progress2 * 2.0f * Mathf.PI),
                    center.y + radius * Mathf.Sin(progress2 * 2.0f * Mathf.PI)
                ));
            }

            GL.End();
        }
    }
}

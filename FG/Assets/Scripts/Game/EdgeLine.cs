using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Линия связи зон. Координаты — в пространстве карты.</summary>
    public sealed class EdgeLine : VisualElement
    {
        public Vector2 A;
        public Vector2 B;

        public EdgeLine()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += OnGenerate;
        }

        private void OnGenerate(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            painter.strokeColor = new Color(0.55f, 0.50f, 0.38f, 0.75f);
            painter.lineWidth = 3f;
            painter.BeginPath();
            painter.MoveTo(A);
            painter.LineTo(B);
            painter.Stroke();
        }
    }
}

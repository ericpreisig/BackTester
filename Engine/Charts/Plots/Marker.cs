using Engine.Helpers;
using System.Drawing;

namespace Engine.Charts.Plots
{
    public class Marker
    {
        public MarkerPosition Position { get; }
        public DateTime Time { get; }
        public string Color { get; set; } = ColorTranslator.ToHtml(System.Drawing.Color.Green);
        public MarkerShape Shape { get; }
        public string Text { get; set; } = "";

        public Marker(DateTime time, MarkerPosition position = MarkerPosition.AboveBar, MarkerShape shape = MarkerShape.Circle)
        {
            Time = time;
            Position = position;
            Shape = shape;
        }
    }

    public enum MarkerPosition
    {
        AboveBar,
        BelowBar,
        InBar,
    }

    public enum MarkerShape
    {
        Circle,
        ArrowUp,
        ArrowDown,
        Square,
    }
}
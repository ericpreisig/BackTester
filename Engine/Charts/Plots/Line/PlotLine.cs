using Engine.Enums;

namespace Engine.Charts.Plots.Line
{
    public class PlotLine : Plot
    {
        public decimal Value { get; }

        public PlotLine(DateTime time, decimal value) : base(time)
        {
            Value = value;
        }
    }
}
using Engine.Enums;

namespace Engine.Charts.Plots.Area
{
    public class PlotArea : Plot
    {
        public decimal Value { get; }

        public PlotArea(DateTime time, decimal value) : base(time)
        {
            Value = value;
        }
    }
}
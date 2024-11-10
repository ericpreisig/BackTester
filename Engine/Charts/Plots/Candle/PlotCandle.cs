using Engine.Enums;

namespace Engine.Charts.Plots.Candle
{
    public class PlotCandle : Plot
    {
        public PlotCandle(DateTime time, decimal open, decimal high, decimal low, decimal close) : base(time)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public decimal Open { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Close { get; }
    }
}
using Engine.Charts.Plots.Line;
using Engine.Enums;

namespace Engine.Charts.Plots.Candle
{
    public class PlotCandleSerie : PlotSerie<PlotCandle, PlotCandleSerieConfig>
    {
        public override PlotTypeEnum Type => PlotTypeEnum.Candle;
        public override PlotCandleSerieConfig Config { get; }

        public PlotCandleSerie(string name, PlotPositionEnum postion, PlotCandleSerieConfig config = null, string chartName = "") : base(name, postion, chartName)
        {
            Config = config ?? new PlotCandleSerieConfig();
        }
    }

    public class PlotCandleSerieConfig : ISerieConfig
    {
        public string UpColor { get; set; } = "#26a69a";
        public string DownColor { get; set; } = "#ef5350";
        public bool BorderVisible { get; set; } = false;
        public string WickUpColor { get; set; } = "#26a69a";
        public string WickDownColor { get; set; } = "#ef5350";
    }
}
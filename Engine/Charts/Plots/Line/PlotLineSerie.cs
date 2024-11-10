using Engine.Charts.Plots.Candle;
using Engine.Enums;

namespace Engine.Charts.Plots.Line
{
    public class PlotLineSerie : PlotSerie<PlotLine, PlotLineSerieConfig>
    {
        public override PlotTypeEnum Type => PlotTypeEnum.Line;
        public override PlotLineSerieConfig Config { get; }

        public PlotLineSerie(string name, PlotPositionEnum postion, PlotLineSerieConfig config = null) : base(name, postion)
        {
            Config = config ?? new PlotLineSerieConfig();
        }
    }

    public class PlotLineSerieConfig : ISerieConfig
    {
        public string Color { get; set; }
    }
}
using Engine.Charts.Plots.Candle;
using Engine.Enums;
using System.Drawing;

namespace Engine.Charts.Plots.Line
{
    public class PlotLineSerie : PlotSerie<PlotLine, PlotLineSerieConfig>
    {
        public override PlotTypeEnum Type => PlotTypeEnum.Line;
        public override PlotLineSerieConfig Config { get; }

        public PlotLineSerie(string name, PlotPositionEnum postion, PlotLineSerieConfig config = null, string chartName = "") : base(name, postion, chartName)
        {
            Config = config ?? new PlotLineSerieConfig();
        }
    }

    public class PlotLineSerieConfig : ISerieConfig
    {
        public PlotLineSerieConfig(Color color) => Color = ColorTranslator.ToHtml(color);
        public PlotLineSerieConfig() { }

        public string Color { get; set; }
    }
}
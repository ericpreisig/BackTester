using Engine.Charts.Plots.Candle;
using Engine.Enums;
using System.Drawing;

namespace Engine.Charts.Plots.Area
{
    public class AreaSerie : PlotSerie<PlotArea, AreaSerieConfig>
    {
        public override PlotTypeEnum Type => PlotTypeEnum.Area;
        public override AreaSerieConfig Config { get; }

        public AreaSerie(string name, PlotPositionEnum postion, AreaSerieConfig config = null, string chartName = "") : base(name, postion, chartName)
        {
            Config = config ?? new AreaSerieConfig();
        }
    }

    public class AreaSerieConfig : ISerieConfig
    {
        public AreaSerieConfig(Color color) => Color = ColorTranslator.ToHtml(color);
        public AreaSerieConfig() { }

        public string Color { get; set; }
    }
}
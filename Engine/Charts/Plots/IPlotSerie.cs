using Engine.Charts.Plots;
using Engine.Enums;

namespace Engine.Charts.Plots
{
    public interface IPlotSerie<out T, out U>
        where T : class, IPlot
        where U : class, ISerieConfig
    {
        public string ChartName { get; set; }
        public string Name { get; set; }
        public PlotTypeEnum Type { get; }
        public PlotPositionEnum Postion { get; set; }
        public IEnumerable<T> Plots { get; }
        public IEnumerable<Marker> Markers { get; }
        public U Config { get; }
        public Dictionary<DateTime, Dictionary<string, string>> Metrics { get; }
    }
}
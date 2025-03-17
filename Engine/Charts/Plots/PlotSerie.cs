using Engine.Charts.Plots;
using Engine.Enums;

namespace Engine.Charts.Plots
{
    public abstract class PlotSerie<T, U> : IPlotSerie<T, U>
        where T : class, IPlot 
        where U : class, ISerieConfig
    {
        public PlotSerie(string name, PlotPositionEnum postion)
        {
            Name = name;
            Postion = postion;
        }

        public string Name { get; set; }
        public abstract PlotTypeEnum Type { get; }
        public PlotPositionEnum Postion { get; set; }
        public IEnumerable<T> Plots { get; } = new List<T>();
        public IEnumerable<Marker> Markers { get; } = new List<Marker>();
        public abstract U Config { get; }
        public void Add(T plot)
        {
            ((List<T>)Plots).Add(plot);
        }

        public void Add(Marker marker)
        {
            ((List<Marker>)Markers).Add(marker);
        }
    }
}
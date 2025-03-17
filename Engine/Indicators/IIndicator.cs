using Engine.Charts;
using Engine.Charts.Plots;

namespace Engine.Indicators
{
    public interface IIndicator
    {
        List<IPlotSerie<IPlot, ISerieConfig>> PlotSeries { get; }
        List<Symbol> Symbols { get; }
        List<IIndicator> Indicators { get; }
        Task UpdateAsync(Symbol symbol, DateTime time);
        Task LoadAsync();
        void OnAfterLoad(EventArgs e);
    }
}
using Engine.Charts;
using Engine.Charts.Plots;

namespace Engine.Indicators
{
    public abstract class BaseIndicator : IIndicator
    {
        public List<IPlotSerie<IPlot, ISerieConfig>> PlotSeries { get; } = new List<IPlotSerie<IPlot, ISerieConfig>>();
        public List<Symbol> Symbols { get; } = new List<Symbol>();
        public List<IIndicator> Indicators { get; } = new List<IIndicator>();

        public void Add(params Symbol[] symbols)
        {
            Symbols.AddRange(symbols);
        }

        public void Add(params IIndicator[] indicators)
        {
            Indicators.AddRange(indicators);
        }

        public PlotSerie<T, U> Add<T, U>(PlotSerie<T, U> serie) 
            where T : class, IPlot
            where U : class, ISerieConfig
        {
            PlotSeries.Add(serie);
            return serie;
        }

        public async Task UpdateAsync(Symbol symbol, DateTime time)
        {
            foreach (var indicator in Indicators)
            {
                await indicator.UpdateAsync(symbol, time);
            }
            var candle = symbol.Next(time);

            if (candle != null)
            {
                await CandelFinishedAsync(candle);
            }
        }

        public abstract Task LoadAsync();
        public abstract Task CandelFinishedAsync(Candle candle);
    }
}

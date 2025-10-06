using Engine.Charts;
using Engine.Charts.Plots;
using Engine.Indicators;

namespace Engine.Core
{
    public class Context
    {
        public Chart Chart { get; }
        public Context(Chart chart)
        {
            Chart = chart;
        }

        public async Task<IEnumerable<IPlotSerie<IPlot, ISerieConfig>>> ExecuteAsync()
        {
            await LoadAsync();

            for (var time = Chart.DateFrom; time < Chart.DateTo; time = time.AddSeconds(Chart.Symbol.Interval.GetSeconds()))
            {
                await Chart.Strategy.UpdateAsync(Chart.Symbol, time);
            }

            return GetPlots(Chart.Strategy);
        }

        private async Task LoadAsync()
        {
            var indicators = new List<IIndicator>();
            indicators.AddRange(await GetIndicators(Chart.Strategy));

            await Chart.Symbol.LoadAsync(Chart.HistoryFrom, Chart.DateTo);
            Chart.Symbol.OnAfterLoad(null);

            foreach (var indicator in indicators)
            {
                foreach (var symbol in indicator.Symbols)
                {
                    await symbol.LoadAsync(Chart.HistoryFrom, Chart.DateTo);
                }
            }
        }

        private async Task<IEnumerable<IIndicator>> GetIndicators(IIndicator indicator, List<IIndicator> previousIndicators = null)
        {
            var indicators = new List<IIndicator>();
            previousIndicators ??= new List<IIndicator>();

            if (previousIndicators.Contains(indicator))
            {
                throw new Exception($"An indicator cannot depends on itself ! {string.Join(" -> ", previousIndicators.Select(a => a.GetType().Name))} -> {indicator.GetType().Name}");
            }

            previousIndicators.Add(indicator);

            await indicator.LoadAsync(Chart.Symbol);

            indicator.OnAfterLoad(null);

            indicators.Add(indicator);

            foreach (var i in indicator.Indicators)
            {
                foreach (var i2 in await GetIndicators(i, previousIndicators))
                {
                    indicators.Add(i2);
                }
            }

            return indicators;
        }

        private IEnumerable<IPlotSerie<IPlot, ISerieConfig>> GetPlots(IIndicator indicator)
        {
            foreach (var serie in indicator.PlotSeries)
                yield return serie;

            foreach (var i in indicator.Indicators)
            {
                foreach (var serie in i.PlotSeries)
                    yield return serie;
            }
        }
    }
}
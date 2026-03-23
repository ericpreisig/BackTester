using Engine.Charts;
using Engine.Charts.Plots;
using Engine.Charts.Plots.Line;
using Engine.Enums;
using Engine.Indicators;

namespace BackTester.Indicators
{
    public class PeltBicIndicator : BaseIndicator
    {
        private PlotLineSerie _price;
        private PlotLineSerie _regim;
        private List<int> _regims;

        public PeltBicIndicator()
        {
            _price = new PlotLineSerie("Price", PlotPositionEnum.OnChart);
            _regim = new PlotLineSerie("Regim", PlotPositionEnum.OnChart);
        }

        public override Task LoadAsync(Symbol symbol)
        {
            symbol.AfterLoad += (_, __) =>
            {
                var candles = symbol.GetAllCandles();
                var returns = candles.Select(a => Math.Log((double)a.Close / (double)a.Open));




            };

            return Task.CompletedTask;
        }

        public override Task CandleFinishedAsync(Candle candle)
        {
            _price.Add(new PlotLine(candle.Time, candle.Close));


            return Task.CompletedTask;
        }
    }
}

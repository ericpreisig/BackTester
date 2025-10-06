using Engine.Charts;
using Engine.Charts.Plots.Line;
using Engine.Enums;
using Engine.Indicators.Core;
using Engine.Strategies;
using Microsoft.AspNetCore.Mvc.Formatters;
using Skender.Stock.Indicators;
using System.Drawing;

namespace BackTester.Strategies
{
    public class BtcSystemStrategy : BaseStrategy
    {
        private PlotLineSerie _slowSma;
        private PlotLineSerie _fastSma;

        private IEnumerable<SmaResult> _fastSmaResult;
        private IEnumerable<SmaResult> _slowSmaResult;

        //private List<Candle> _history = new List<Candle>();

        public BtcSystemStrategy(decimal capital) : base(capital)
        {
            Add(_slowSma = new PlotLineSerie("SlowSma", PlotPositionEnum.OnChart, new PlotLineSerieConfig(Color.Red)));
            Add(_fastSma = new PlotLineSerie("FastSma", PlotPositionEnum.OnChart, new PlotLineSerieConfig(Color.Blue)));
        }

        public override async Task LoadAsync(Symbol symbol)
        {
            symbol.AfterLoad += (_, _) =>
            {
                _slowSmaResult = symbol.GetAllCandles().ToSma(100);
                _fastSmaResult = symbol.GetAllCandles().ToSma(20);
            };
        }

        public override async Task CandleFinishedAsync(Candle candle)
        {
            var slowSma = _slowSmaResult.Single(a => a.Timestamp == candle.Timestamp).Sma;
            var fastSma = _fastSmaResult.Single(a => a.Timestamp == candle.Timestamp).Sma;

            if (slowSma != null && fastSma != null)
            {
                _slowSma.Add(new PlotLine(candle.Time, (decimal)slowSma));
                _fastSma.Add(new PlotLine(candle.Time, (decimal)fastSma));

                var slowValues = _slowSma.Plots.TakeLast(2).Select(a => a.Value);
                var fastValues = _fastSma.Plots.TakeLast(2).Select(a => a.Value);

                if (slowValues.FirstOrDefault() > fastValues.FirstOrDefault() && slowValues.LastOrDefault() < fastValues.LastOrDefault())
                {
                    BuyFull(candle);

                }
                if (slowValues.FirstOrDefault() < fastValues.FirstOrDefault() && slowValues.LastOrDefault() > fastValues.LastOrDefault())
                {
                    SellFull(candle);

                }
            }       
        }
    }
}

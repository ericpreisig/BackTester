using Engine.Charts;
using Engine.Charts.Plots.Line;
using Engine.Indicators.Core;
using Engine.Strategies;
using System.Drawing;

namespace BackTester.Strategies
{
    public class SmaCrossingStrategy : BaseStrategy
    {
        private SmaIndicator _slow;
        private SmaIndicator _fast;
        public PlotLineSerie SolPrice;

        public SmaCrossingStrategy(decimal capital) : base(capital)
        {
        }

        public override async Task LoadAsync()
        {
            Add(_slow = new SmaIndicator(new SmaConfig(20, Color.Red)));
            Add(_fast = new SmaIndicator(new SmaConfig(100, Color.Blue)));
        }

        public override async Task CandleFinishedAsync(Candle candle)
        {
            var slowValues = _slow.Average.Plots.TakeLast(2).Select(a => a.Value);
            var fastValues = _fast.Average.Plots.TakeLast(2).Select(a => a.Value);

            if (slowValues.FirstOrDefault() > fastValues.FirstOrDefault() && slowValues.LastOrDefault() < fastValues.LastOrDefault())
            {
                SellFull(candle);
            }
            if (slowValues.FirstOrDefault() < fastValues.FirstOrDefault() && slowValues.LastOrDefault() > fastValues.LastOrDefault())
            {
                BuyFull(candle);
            }
        }
    }
}

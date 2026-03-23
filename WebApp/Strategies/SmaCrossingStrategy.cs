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
        private SmaIndicator _mid;

        public SmaCrossingStrategy(decimal capital) : base(capital)
        {
        }

        public override async Task LoadAsync(Symbol symbol)
        {
            Add(_slow = new SmaIndicator(20, Color.Red));
            Add(_fast = new SmaIndicator(100, Color.Blue));

            Add(_mid = new SmaIndicator(50, Color.Yellow));

            _slow.AfterLoad += (_, _)=> _slow.Average.Name = "Slow SMA";
            _fast.AfterLoad += (_, _)=> _fast.Average.Name = "Fast SMA";
            _mid.AfterLoad += (_, _)=> {
                _mid.Average.Name = "Mid SMA";
                _mid.Average.Postion = Engine.Enums.PlotPositionEnum.UnderChart;
            };
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

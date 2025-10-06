using Engine.Charts;
using Engine.Indicators;

namespace Engine.Strategies
{
    public class DefaultStrategy : BaseStrategy
    {
        public DefaultStrategy(params IIndicator[] indicators)
        {
            Add(indicators);
        }

        public override Task LoadAsync(Symbol symbol) => Task.CompletedTask;

        public override Task CandleFinishedAsync(Candle candle) => Task.CompletedTask;
    }
}

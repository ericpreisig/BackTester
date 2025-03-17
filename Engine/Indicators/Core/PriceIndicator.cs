using Engine.Charts;
using Engine.Charts.Plots.Candle;
using Engine.Enums;

namespace Engine.Indicators.Core
{
    public class PriceIndicator : BaseIndicator
    {
        public PlotCandleSerie Price;

        public override Task LoadAsync() 
        {
            Add(Price = new PlotCandleSerie(nameof(PriceIndicator), PlotPositionEnum.OnChart));
            return Task.CompletedTask;
        }

        public override Task CandleFinishedAsync(Candle candle)
        {
            Price.Add(new PlotCandle(candle.Time, candle.Open, candle.High, candle.Low, candle.Close));

            return Task.CompletedTask;
        }
    }
}

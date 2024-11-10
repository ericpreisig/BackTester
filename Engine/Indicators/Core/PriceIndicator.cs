using Engine.Charts;
using Engine.Charts.Plots.Candle;
using Engine.Charts.Plots.Line;
using Engine.Enums;

namespace Engine.Indicators.Core
{
    public class PriceIndicator : BaseIndicator
    {
        private PlotCandleSerie _price;

        public override Task LoadAsync() 
        {
            Add(_price = new PlotCandleSerie(nameof(PriceIndicator), PlotPositionEnum.OnChart));
            return Task.CompletedTask;
        }

        public override Task CandelFinishedAsync(Candle candle)
        {
            _price.Add(new PlotCandle(candle.Time, candle.Open, candle.High, candle.Low, candle.Close));
            return Task.CompletedTask;
        }
    }
}

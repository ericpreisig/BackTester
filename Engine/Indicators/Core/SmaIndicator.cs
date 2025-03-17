using Engine.Charts;
using Engine.Charts.Plots.Line;
using Engine.Enums;
using System.Drawing;

namespace Engine.Indicators.Core
{
    public class SmaIndicator : BaseIndicator
    {
        private readonly SmaConfig _config;
        private readonly List<Candle> _candles = new List<Candle>();
        public PlotLineSerie Average;

        public SmaIndicator(SmaConfig config)
        {
            _config = config;
        }

        public override Task LoadAsync() 
        {
            Add(Average = new PlotLineSerie(nameof(SmaIndicator), PlotPositionEnum.OnChart, new PlotLineSerieConfig(_config.Color)));
            
            return Task.CompletedTask;
        }

        public override Task CandleFinishedAsync(Candle candle)
        {
            _candles.Add(candle);

            if(_candles.Count == _config.Length)
            {
                Average.Add(new PlotLine(candle.Time, _candles.Average(a => a.Close)));
                _candles.RemoveAt(0);
            }
            return Task.CompletedTask;
        }
    }

    public record SmaConfig(int Length, Color Color = default);
}

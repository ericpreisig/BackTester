using Engine.Charts;
using Engine.Charts.Plots.Line;
using Engine.Enums;
using System.Drawing;

namespace Engine.Indicators.Core
{
    public class SmaIndicator : BaseIndicator
    {
        private readonly List<Candle> _candles = new List<Candle>();
        public PlotLineSerie Average;

        public int Length { get; set; }
        public Color Color { get; set; }

        public SmaIndicator(int length, Color color = default)
        {
            Length = length;
            Color = color;
        }

        public override Task LoadAsync(Symbol symbol) 
        {
            Add(Average = new PlotLineSerie(nameof(SmaIndicator), PlotPositionEnum.OnChart, new PlotLineSerieConfig(Color)));
            
            return Task.CompletedTask;
        }

        public override Task CandleFinishedAsync(Candle candle)
        {
            _candles.Add(candle);

            if(_candles.Count == Length)
            {
                Average.Add(new PlotLine(candle.Time, _candles.Average(a => a.Close)));
                _candles.RemoveAt(0);
            }
            return Task.CompletedTask;
        }
    }
}

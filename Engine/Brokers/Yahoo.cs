using Engine.Charts;
using Engine.Enums;
using YahooFinanceApi;
using Candle = Engine.Charts.Candle;

namespace Engine.Brokers
{
    public class Yahoo : IBroker
    {
        public async Task<List<Candle>> GetDataFeedAsync(string code, Interval interval, DateTime from, DateTime to)
        {
            var period = Period.Daily;

            switch (interval.IntervalHorizon)
            {
                case IntervalEnum.Daily:
                    period = Period.Daily;
                    break;
                case IntervalEnum.Weekly:
                    period = Period.Weekly;
                    break;
                case IntervalEnum.Monthly:
                    period = Period.Monthly;
                    break;
            }

            var history = await YahooFinanceApi.Yahoo.GetHistoricalAsync(code, from, to, period);

            return history.Select(a => new Candle()
            {
                Close = a.Close,
                High = a.High,
                Low = a.Low,
                Open = a.Open,
                Time = a.DateTime,
                Volume = a.Volume
            }).ToList();
        }
    }
}
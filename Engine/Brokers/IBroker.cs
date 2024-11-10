using Engine.Charts;

namespace Engine.Brokers
{
    public interface IBroker
    {
        Task<List<Candle>> GetDataFeedAsync(string code, Interval interval, DateTime from, DateTime to);
    }
}
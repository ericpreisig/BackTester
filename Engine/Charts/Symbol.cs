using Engine.Brokers;

namespace Engine.Charts
{
    public class Symbol
    {
        private List<Candle> _candles;
        private int _index;
        private string _code { get; set; }
        private IBroker _broker { get; set; }
        public Interval Interval { get; init; } = new Interval(Enums.IntervalEnum.Daily);
        public event EventHandler AfterLoad;

        public Symbol(string code) : this(code, new Yahoo()) { }
        public Symbol(string code, IBroker broker)
        {
            _code = code;
            _broker = broker;
        }

        public async Task LoadAsync(DateTime from, DateTime to)
        {
            _candles = await _broker.GetDataFeedAsync(_code, Interval, from, to);
        }

        public void OnAfterLoad(EventArgs e)
        {
            AfterLoad?.Invoke(this, e);
        }

        public List<Candle> GetAllCandles()
        {
            return _candles;
        }
        
        public Candle Next(DateTime time)
        {
            if (_candles == null)
            {
                throw new Exception($"{_code} must be loaded before use !");
            }

            if (_candles[0].Time > time)
            {
                return null;
            }

            for (; _index < _candles.Count; _index++)
            {
                if (_candles[_index].Time >= time)
                {
                    return _candles[_index];
                }
            }

            return null;
        }
    }
}
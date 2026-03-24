using Engine.Brokers;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

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
            var allCandles = await _broker.GetDataFeedAsync(_code, Interval, from, to);
            _candles = allCandles.Where(a => a.Close > 0).ToList();
            _candles = _candles.Take(_candles.Count-1).ToList();


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
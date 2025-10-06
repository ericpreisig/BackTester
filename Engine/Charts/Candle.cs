using Engine.Enums;
using Skender.Stock.Indicators;

namespace Engine.Charts
{
    public record Candle : IQuote
    {
        public DateTime Time { get; init; }
        public decimal Open { get; init; }
        public decimal High { get; init; }
        public decimal Low { get; init; }
        public decimal Close { get; init; }
        public decimal Volume { get; init; }

        public double Value => (double)Close;

        public DateTime Timestamp => Time;

        /// <summary>
        /// Gets the price at a point in the candle's lifetime.
        /// </summary>
        /// <param name="atTime"></param>
        /// <returns>Price at the specified time</returns>
        internal decimal GetPrice(PriceTimeEnum atTime)
        {
            switch (atTime)
            {
                case PriceTimeEnum.AtOpen:
                    return Open;
                case PriceTimeEnum.AtHigh:
                    return High;
                case PriceTimeEnum.AtLow:
                    return Low;
                case PriceTimeEnum.AtClose:
                    return Close;
                default:
                    throw new ArgumentException("Invalid PriceTime");
            }
        }
    }
}
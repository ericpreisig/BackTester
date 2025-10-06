using Engine.Charts;
using Engine.Charts.Plots;
using Engine.Charts.Plots.Area;
using Engine.Charts.Plots.Line;
using Engine.Core;
using Engine.Indicators;
using Engine.Indicators.Core;

namespace Engine.Strategies
{

    public abstract class BaseStrategy : BaseIndicator, IStrategy
    {
        protected Portfolio Portfolio { get; private set; }
        private Portfolio _buyAndHoldPortfolio;
        private PriceIndicator _priceIndicator;
        private PlotLineSerie _equity;
        private AreaSerie _buyAndHold;

        protected BaseStrategy(decimal capital) : this()
        {
            Portfolio = new Portfolio(capital);
            _buyAndHoldPortfolio = new Portfolio(capital);
        }

        protected BaseStrategy()
        {
            Add(_priceIndicator = new PriceIndicator());
            Add(_equity = new PlotLineSerie("Equity", Enums.PlotPositionEnum.UnderChart, chartName: "Portfolio"));
            Add(_buyAndHold = new AreaSerie("BuyAndHold", Enums.PlotPositionEnum.UnderChart, chartName: "Portfolio"));

            _priceIndicator.AfterCandle += (s, e) =>
            {
                CalculateEquity(e.Candle);
            };
        }

        private void CalculateEquity(Candle candle)
        {
            if(_buyAndHoldPortfolio.Cash != 0 && candle.Close != 0)
            {
                var quantity = _buyAndHoldPortfolio.Cash / candle.Close;
                _buyAndHoldPortfolio.AddOrder(new Order()
                {
                    Action = Enums.ActionEnum.Buy,
                    CurrentPrice = candle.Close,
                    Quantity = quantity
                });
            }

            var value = Portfolio.Cash + Portfolio.Orders.Sum(a => a.Quantity) * candle.Close;
            var valueHold = _buyAndHoldPortfolio.Cash + _buyAndHoldPortfolio.Orders.Sum(a => a.Quantity) * candle.Close;

            _equity.Add(new PlotLine(candle.Time, value));
            _buyAndHold.Add(new PlotArea(candle.Time, valueHold));
        }

        public void BuyQuantity(Candle candle, decimal quantity)
        {
            _priceIndicator.Price.Add(new Marker(candle.Time, MarkerPosition.BelowBar, MarkerShape.ArrowUp));
            Portfolio.AddOrder(new Order()
            {
                Action = Enums.ActionEnum.Buy,
                CurrentPrice = candle.Close,
                Quantity = quantity
            });
        }

        public void BuyAmount(Candle candle, decimal amount)
        {
            if(candle.Close != 0)
            {
                var quantity = amount / candle.Close;
                BuyQuantity(candle, quantity);
            }  
        }

        public void SellQuantity(Candle candle, decimal quantity)
        {
            _priceIndicator.Price.Add(new Marker(candle.Time, MarkerPosition.AboveBar, MarkerShape.ArrowDown));
            Portfolio.AddOrder(new Order()
            {
                Action = Enums.ActionEnum.Sell,
                CurrentPrice = candle.Close,
                Quantity = -quantity
            });
        }

        public void SellAmount(Candle candle, decimal amount)
        {
            var quantity = amount / candle.Close;
            SellQuantity(candle, quantity);
        }

        public void SellFull(Candle candle)
        {
            var maxQuantity = Portfolio.Orders.Sum(a => a.Quantity);
            SellQuantity(candle, maxQuantity);
        }

        public void BuyFull(Candle candle)
        {
            BuyAmount(candle, Portfolio.Cash);
        }
    }
}

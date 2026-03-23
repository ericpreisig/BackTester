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

        private decimal _maxEquity = 0;
        private decimal _maxDrawdown = 0;
        private decimal _maxDrawdownDollar = 0;
        private decimal _bhMaxEquity = 0;
        private decimal _bhMaxDrawdown = 0;
        private decimal _stratPreviousEquity = 0;
        private decimal _bhPreviousEquity = 0;

        // Welford CAPM & MPT Trackers
        private int _covCount = 0;
        private decimal _stratMean = 0;
        private decimal _marketMean = 0;
        private decimal _coMom = 0;
        private decimal _marketM2 = 0;
        private decimal _stratM2 = 0;
        
        // Asymmetry Trackers
        private decimal _sumSquaredDownside = 0;
        private decimal _sumGains = 0;
        private decimal _sumLosses = 0;

        // Info Ratio Trackers
        private decimal _activeMean = 0;
        private decimal _activeM2 = 0;

        private decimal _initialCapital = 0;

        protected BaseStrategy(decimal capital) : this()
        {
            _initialCapital = capital;
            _stratPreviousEquity = capital;
            _bhPreviousEquity = capital;
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
            if(_buyAndHoldPortfolio.Orders.Count == 0 && candle.Close != 0 && _buyAndHoldPortfolio.Cash > 0)
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

            // Metric tracking chronologically
            if (!_equity.Metrics.ContainsKey(candle.Time))
            {
                _equity.Metrics[candle.Time] = new Dictionary<string, string>();
            }

            // Strategy returns
            decimal pnl = value - _initialCapital;
            decimal pnlPercent = _initialCapital > 0 ? (pnl / _initialCapital) * 100 : 0;

            if (value > _maxEquity) _maxEquity = value;
            decimal drawdownDollar = _maxEquity - value;
            if (drawdownDollar > _maxDrawdownDollar) _maxDrawdownDollar = drawdownDollar;
            decimal drawdown = _maxEquity > 0 ? (drawdownDollar / _maxEquity) * 100 : 0;
            if (drawdown > _maxDrawdown) _maxDrawdown = drawdown;

            // Buy and Hold returns
            decimal bhPnl = valueHold - _initialCapital;
            decimal bhPnlPercent = _initialCapital > 0 ? (bhPnl / _initialCapital) * 100 : 0;

            if (valueHold > _bhMaxEquity) _bhMaxEquity = valueHold;
            decimal bhDrawdown = _bhMaxEquity > 0 ? ((_bhMaxEquity - valueHold) / _bhMaxEquity) * 100 : 0;
            if (bhDrawdown > _bhMaxDrawdown) _bhMaxDrawdown = bhDrawdown;

            // --- MODERN PORTFOLIO THEORY (MPT) TRACKERS ---
            decimal stratRet = _stratPreviousEquity > 0 ? (value - _stratPreviousEquity) / _stratPreviousEquity : 0;
            decimal marketRet = _bhPreviousEquity > 0 ? (valueHold - _bhPreviousEquity) / _bhPreviousEquity : 0;
            _stratPreviousEquity = value;
            _bhPreviousEquity = valueHold;

            _covCount++;
            
            // Welford Co-variance (Beta) & Variance (Sharpe)
            decimal stratDelta = stratRet - _stratMean;
            _stratMean += stratDelta / _covCount;
            decimal marketDelta = marketRet - _marketMean;
            _marketMean += marketDelta / _covCount;
            
            _coMom += stratDelta * (marketRet - _marketMean);
            _marketM2 += marketDelta * (marketRet - _marketMean);
            _stratM2 += stratDelta * (stratRet - _stratMean);

            // Welford Active Return (Info Ratio)
            decimal activeReturn = stratRet - marketRet;
            decimal actDelta = activeReturn - _activeMean;
            _activeMean += actDelta / _covCount;
            _activeM2 += actDelta * (activeReturn - _activeMean);

            // Sortino & Omega Trackers
            if (stratRet > 0) _sumGains += stratRet;
            else if (stratRet < 0) 
            {
                _sumLosses += Math.Abs(stratRet);
                _sumSquaredDownside += (stratRet * stratRet);
            }

            // --- COMPUTATIONS ---
            decimal marketVariance = _covCount > 1 ? _marketM2 / (_covCount - 1) : 0;
            decimal stratVariance = _covCount > 1 ? _stratM2 / (_covCount - 1) : 0;
            decimal covariance = _covCount > 1 ? _coMom / (_covCount - 1) : 0;
            
            decimal beta = marketVariance > 0 ? covariance / marketVariance : 1;
            decimal jensenAlpha = _stratMean - (beta * _marketMean); // Assuming Rf = 0

            decimal stratStdDev = (decimal)Math.Sqrt((double)stratVariance);
            decimal sharpeRatio = stratStdDev > 0 ? _stratMean / stratStdDev : 0;

            decimal downsideVariance = _covCount > 0 ? _sumSquaredDownside / _covCount : 0;
            decimal downsideDev = (decimal)Math.Sqrt((double)downsideVariance);
            decimal sortinoRatio = downsideDev > 0 ? _stratMean / downsideDev : 0;

            decimal omegaRatio = _sumLosses > 0 ? _sumGains / _sumLosses : 999;
            decimal calmarRatio = _maxDrawdown > 0 ? (pnlPercent / 100m) / (_maxDrawdown / 100m) : 999;

            decimal trackingVariance = _covCount > 1 ? _activeM2 / (_covCount - 1) : 0;
            decimal trackingError = (decimal)Math.Sqrt((double)trackingVariance);
            decimal informationRatio = trackingError > 0 ? _activeMean / trackingError : 0;

            // Retained Core Stats
            int totalTrades = Portfolio.WinningTrades + Portfolio.LosingTrades;
            decimal winRate = totalTrades > 0 ? ((decimal)Portfolio.WinningTrades / totalTrades) : 0;
            decimal lossRate = totalTrades > 0 ? 1m - winRate : 0;
            
            decimal avgWin = Portfolio.WinningTrades > 0 ? Portfolio.GrossProfit / Portfolio.WinningTrades : 0;
            decimal avgLoss = Portfolio.LosingTrades > 0 ? Portfolio.GrossLoss / Portfolio.LosingTrades : 0;
            decimal payoffRatio = avgLoss == 0 ? (avgWin > 0 ? 999 : 0) : avgWin / avgLoss;
            
            decimal profitFactor = Portfolio.GrossLoss == 0 ? (Portfolio.GrossProfit > 0 ? 999 : 0) : Portfolio.GrossProfit / Portfolio.GrossLoss;
            decimal kellyFraction = payoffRatio > 0 ? winRate - (lossRate / payoffRatio) : 0;
            
            decimal exposure = value > 0 ? ((Portfolio.Position * candle.Close) / value) * 100 : 0;

            var metrics = _equity.Metrics[candle.Time];
            
            // 🧠 Modern Portfolio Theory (MPT) Metrics
            metrics["Sharpe Ratio"] = sharpeRatio.ToString("0.000");
            metrics["Sortino Ratio"] = sortinoRatio.ToString("0.000");
            metrics["Omega Ratio"] = omegaRatio.ToString("0.00");
            metrics["Calmar Ratio"] = calmarRatio.ToString("0.00");
            metrics["Beta (β)"] = beta.ToString("0.00");
            metrics["Jensen's Alpha (α)"] = (jensenAlpha * 100m).ToString("0.000") + "%";
            metrics["Information Ratio"] = informationRatio.ToString("0.000");
            metrics["Kelly Criterion"] = (kellyFraction * 100m).ToString("0.00") + "%";

            // 📈 Récapitulatif (Pur)
            metrics["Net Profit / B&H"] = $"{pnlPercent:0.00}% / {bhPnlPercent:0.00}%";
            metrics["Strat DD / B&H DD"] = $"{drawdown:0.00}% / {bhDrawdown:0.00}%";
            metrics["Max DD / B&H Max"] = $"{_maxDrawdown:0.00}% / {_bhMaxDrawdown:0.00}%";
            
            // 🎯 Trades
            metrics["Win Rate"] = (winRate * 100m).ToString("0.00") + "%";
            metrics["Profit Factor"] = profitFactor.ToString("0.00");
            metrics["Exposure (%)"] = exposure.ToString("0.00") + "%";
            metrics["Market Position"] = Portfolio.Position.ToString("0.00000");
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

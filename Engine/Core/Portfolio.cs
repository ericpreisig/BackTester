using Engine.Strategies;

namespace Engine.Core
{
    public class Portfolio
    {
        public Portfolio(decimal cash) => Cash = cash;

        public decimal Cash { get; private set; }
        public List<Order> Orders { get; } = new List<Order>();

        public decimal Position => Orders.Sum(o => o.Quantity);
        
        public decimal AverageEntryPrice { get; private set; }
        public decimal RealizedPNL { get; private set; }
        
        public int WinningTrades { get; private set; }
        public int LosingTrades { get; private set; }

        public List<decimal> ClosedTradesPnl { get; } = new List<decimal>();
        public decimal GrossProfit { get; private set; }
        public decimal GrossLoss { get; private set; }
        public int CurrentWinStreak { get; private set; }
        public int CurrentLossStreak { get; private set; }
        public int MaxWinStreak { get; private set; }
        public int MaxLossStreak { get; private set; }

        public void AddOrder(Order order)
        {
            decimal currentPos = Position;
            decimal fillQty = Math.Abs(order.Quantity);
            decimal fillPrice = order.CurrentPrice;

            if(order.Action == Enums.ActionEnum.Buy)
            {
                if (currentPos >= 0)
                {
                    decimal totalCost = (currentPos * AverageEntryPrice) + (fillQty * fillPrice);
                    AverageEntryPrice = currentPos + fillQty > 0 ? totalCost / (currentPos + fillQty) : 0;
                }
                Cash -= fillPrice * fillQty;
            }
            else
            {
                decimal realized = (fillPrice - AverageEntryPrice) * fillQty;
                RealizedPNL += realized;
                ClosedTradesPnl.Add(realized);

                if (realized > 0) 
                {
                    WinningTrades++;
                    GrossProfit += realized;
                    CurrentWinStreak++;
                    CurrentLossStreak = 0;
                    if (CurrentWinStreak > MaxWinStreak) MaxWinStreak = CurrentWinStreak;
                }
                else if (realized < 0) 
                {
                    LosingTrades++;
                    GrossLoss += Math.Abs(realized);
                    CurrentLossStreak++;
                    CurrentWinStreak = 0;
                    if (CurrentLossStreak > MaxLossStreak) MaxLossStreak = CurrentLossStreak;
                }

                if (currentPos - fillQty <= 0) AverageEntryPrice = 0;
                
                Cash += fillPrice * fillQty;
            }

            Orders.Add(order);
        }
    }
}

using Engine.Strategies;

namespace Engine.Core
{
    public class Portfolio
    {
        public Portfolio(decimal cash) => Cash = cash;

        public decimal Cash { get; private set; }
        public List<Order> Orders { get; } = new List<Order>();

        public void AddOrder(Order order)
        {
            if(order.Action == Enums.ActionEnum.Buy)
            {
                //if(Cash - order.CurrentPrice * order.Quantity < 0)
                //{
                //    throw new Exception("Cannot buy");
                //}

                Cash-= order.CurrentPrice * order.Quantity;
            }
            else
            {
                Cash += order.CurrentPrice * -order.Quantity;
            }

            Orders.Add(order);
        }
    }
}

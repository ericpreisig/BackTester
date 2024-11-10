using Engine.Indicators;

namespace Engine.Strategies
{

    public abstract class BaseStrategy : BaseIndicator, IStrategy
    {
        public List<Order> Orders { get; } = new List<Order>();

        public void Buy()
        {

        }

        public void Sell()
        {

        }
    }
}

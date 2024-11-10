using Engine.Indicators;

namespace Engine.Strategies
{
    public interface IStrategy : IIndicator
    {
        List<Order> Orders { get; }
    }
}
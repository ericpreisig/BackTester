using Engine.Enums;

namespace Engine.Strategies
{
    public class Order
    {
        ActionEnum Action { get; }
        decimal CurrentPrice { get; }
        decimal Amount { get; }
        decimal Quantity { get; }
    }
}
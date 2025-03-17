using Engine.Enums;

namespace Engine.Strategies
{
    public class Order
    {
        public ActionEnum Action { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Quantity { get; set; }
    }
}
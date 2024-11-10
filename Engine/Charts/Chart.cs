using Engine.Strategies;

namespace Engine.Charts
{
    public class Chart
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; } = DateTime.Now;
        public Symbol Symbol { get; set; }
        public IStrategy Strategy { get; set; }
    }
}
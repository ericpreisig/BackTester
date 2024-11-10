using Engine.Charts.Plots;
using Engine.Enums;

namespace Engine.Charts.Plots
{
    public abstract class Plot : IPlot
    {
        public Plot(DateTime time)
        {
            Time = time;
        }

        public DateTime Time { get; }
    }
}
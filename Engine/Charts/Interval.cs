using Engine.Enums;

namespace Engine.Charts
{
    public record Interval(IntervalEnum IntervalHorizon, int IntervalValue = 1)
    {

        public int GetSeconds()
        {
            switch (IntervalHorizon)
            {
                case IntervalEnum.Daily:
                    return 60 * 60 * 24 * IntervalValue;
            }

            throw new Exception($"{IntervalHorizon} not supported");
        }
    }
}
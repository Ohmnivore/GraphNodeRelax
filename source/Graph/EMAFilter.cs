using UnityEngine;

namespace GraphNodeRelax
{
    // https://stackoverflow.com/a/50854247
    public struct EMAFilter
    {
        public const float DefaultFactor = 3;

        public Vector2 Average { get; private set; }
        float Counter;
        float Factor;

        public EMAFilter(float factor)
        {
            Average = Vector2.zero;
            Counter = 0f;
            Factor = factor;
        }

        public void Add(Vector2 value)
        {
            Counter += 1f;

            Average = Average + (value - Average) / Mathf.Min(Counter, Factor);
        }

        public void Reset()
        {
            Average = Vector2.zero;
            Counter = 0f;
        }

        public void Reset(Vector2 value)
        {
            Average = value;
            Counter = Factor;
        }
    }
}

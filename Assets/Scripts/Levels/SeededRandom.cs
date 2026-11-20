namespace Skp.Levels
{
    /// <summary>
    /// The prototype's xorshift32 generator, bit for bit: rng(seed) in reference/prototype/index.html.
    /// Same seed, same sequence, so the Unity generator reproduces the prototype's levels exactly.
    /// </summary>
    public class SeededRandom
    {
        uint _s;

        public SeededRandom(double seed)
        {
            // JS: (seed >>> 0) || 1
            _s = (uint)(long)seed;
            if (_s == 0) _s = 1;
        }

        /// <summary>Next value in [0, 1).</summary>
        public double Next()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s / 4294967296.0;
        }
    }
}

using System;
using System.Threading;
using HpTimeStamps;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
// ReSharper disable LocalizableElement
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;
    using StampSource = MonotonicTimeStampUtil<MonotonicContext>;

    public class PortableSerializationTestFixture
    {
        public (PortableMonotonicStamp RandomStamp, PortableDuration RandomDuration) RandomPair
        {
            get
            {
                PortableDuration d = RandomDuration;
                return (d + TheBasisStamp, d);
            }
        }

        public PortableMonotonicStamp RandomStamp => TheBasisStamp + RandomDuration;

        public PortableDuration RandomDuration
        {
            get
            {
                long seconds = Rng.NextLong(-SecondsPerFiveHundredYears, SecondsPerFiveHundredYears + 1);
                int nanoSeconds = Rng.Next(-999_999_999, 1_000_000_000);
                PortableDuration dur = PortableDuration.FromSeconds(seconds);
                dur += PortableDuration.FromNanoseconds(nanoSeconds);
                return dur;
            }
        }



        private const long SecondsPerYear = 31_536_000;
        private const long SecondsPerFiveHundredYears = SecondsPerYear * 5;

        private Random Rng => TheRng.Value!;
        private static readonly PortableMonotonicStamp TheBasisStamp = (PortableMonotonicStamp) StampSource.StampNow;    
        private static readonly ThreadLocal<Random> TheRng = new ThreadLocal<Random>(() => new Random(), false);
    }

    public static class RandomExtensionMethods
    {
        /// <summary>
        /// Returns a random long from min (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="min">The inclusive minimum bound</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than min</param>
        public static long NextLong(this Random random, long min, long max)
        {
            if (max <= min)
                throw new ArgumentOutOfRangeException("max", "max must be > min!");

            //Working with ulong so that modulo works correctly with values > long.MaxValue
            ulong uRange = (ulong)(max - min);

            //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
            //for more information.
            //In the worst case, the expected number of calls is 2 (though usually it's
            //much closer to 1) so this loop doesn't really hurt performance at all.
            ulong ulongRand;
            Span<byte> buf = stackalloc byte[8];
            do
            {
                random.NextBytes(buf);
                ulongRand = (ulong)BitConverter.ToInt64(buf);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long)(ulongRand % uRange) + min;
        }

        /// <summary>
        /// Returns a random long from 0 (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
        public static long NextLong(this Random random, long max)
        {
            return random.NextLong(0, max);
        }

        /// <summary>
        /// Returns a random long over all possible values of long (except long.MaxValue, similar to
        /// random.Next())
        /// </summary>
        /// <param name="random">The given random instance</param>
        public static long NextLong(this Random random)
        {
            return random.NextLong(long.MinValue, long.MaxValue);
        }
    }
}

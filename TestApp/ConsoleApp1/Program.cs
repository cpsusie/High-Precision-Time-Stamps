using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using HpTimeStamps;
using JetBrains.Annotations;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace TestApp
{
    using MonotonicSource = MonotonicTimeStampUtil<MonotonicContext>;
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;

    class Program
    {
        static void Main()
        {

            Console.WriteLine("System information:");
            Console.WriteLine("TimeSpan ticks per second: {0:N0}", TimeSpan.TicksPerSecond);
            Console.WriteLine("Duration ticks per second: {0:N0}", Duration.TicksPerSecond);
            Console.WriteLine("Portable duration ticks per second: {0:N0}", PortableDuration.TicksPerSecond);
            Console.WriteLine("Local reference time: [{0:O}].", MonotonicSource.StampContext.LocalTimeBeginReference);
            Console.WriteLine("Utc reference time: [{0:O}].", MonotonicSource.StampContext.UtcDateTimeBeginReference);
            Console.WriteLine("Local utc offset: [{0:N3}] hours.", MonotonicSource.StampContext.UtcLocalTimeOffset.TotalHours);
            Console.WriteLine("Stopwatch tick equivalent to local time: [{0:N}]",
                MonotonicSource.StampContext.ReferenceTicks);
            Console.WriteLine("Easy conversion all around: [{0}]", MonotonicSource.StampContext.EasyConversionAllWays);
            Console.WriteLine("Easy conversions between stopwatch ticks and timespan ticks: [{0}]", MonotonicSource.StampContext.EasyConversionToAndFromTimespanTicks);
            Console.WriteLine("Easy conversions between stopwatch ticks and nanoseconds: [{0}]", MonotonicSource.StampContext.EasyConversionToAndFromNanoseconds);

            Console.WriteLine("Duration frequency: {0:N0} ticks per second.", Duration.TicksPerSecond);
            Console.WriteLine("Timespan frequency: {0:N0} ticks per second.", TimeSpan.TicksPerSecond);
            MonotonicStamp now = MonotonicSource.StampNow;
            Console.WriteLine("Initial local stamp: [{0:O}].", now.ToLocalDateTime());
            Console.WriteLine("Initial utc stamp: [{0:O}].", now.ToUtcDateTime());

            Console.WriteLine("Earliest instant representable as a monotonic timestamp in this process: [{0}].", MonotonicStamp.MinValue);
            Console.WriteLine("Same value converted to a UTC DateTime: [{0:O}].", MonotonicStamp.MinValue.ToUtcDateTime());
            Console.WriteLine("Same value converted to a Local DateTime: [{0:O}].", MonotonicStamp.MinValue.ToLocalDateTime());
            Console.WriteLine("Latest instant representable as a monotonic timestamp in this process: [{0}]", MonotonicStamp.MaxValue);
            Console.WriteLine("Same value converted to a UTC DateTime: [{0:O}].", MonotonicStamp.MaxValue.ToUtcDateTime());
            Console.WriteLine("Same value converted to a Local DateTime: [{0:O}].", MonotonicStamp.MaxValue.ToLocalDateTime());

            Console.WriteLine("DONE SYSTEM INFO.");
            Console.WriteLine();

            Console.WriteLine("Begin hp timestamp test.");
            TestHpTimestamps();
            Console.WriteLine("End hp timestamp test.");
            Console.WriteLine();

            Console.WriteLine("Begin monotonic timestamp test.");
            TestMonotonicTimestamps();
            Console.WriteLine("End monotonic timestamp tests.");

            Console.WriteLine("DONE");
        }

        static void TestMonotonicTimestamps()
        {
            ref readonly MonotonicContext
                stampContext = ref MonotonicSource.StampContext;
            Deny(stampContext.IsInvalid, "The stamp context should not be invalid");
            Console.WriteLine("Stamp context: [" + stampContext + "].");
            
            MonotonicStamp now = MonotonicSource.StampNow;
            var (utcReferenceTime, offsetFromReference, offsetFromUtcReference) = now.Value;
            DateTime dtLocalNow = now.ToLocalDateTime();
            Assert((dtLocalNow - offsetFromUtcReference) == now.ToUtcDateTime(),
                "Subtract offset to get utc from local.");
            Console.WriteLine(
                $"now reference time: {utcReferenceTime:O}, elapsed since reference: {offsetFromReference.TotalMilliseconds:F4} milliseconds, local utc offset: {offsetFromUtcReference.TotalHours:N1} hours");
            
            MonotonicStamp nowPlusX = MonotonicSource.StampNow;
            Duration diff = nowPlusX - now;
            Console.WriteLine("difference in milliseconds: [" + diff.TotalMilliseconds.ToString("N6") + "].");
            Assert(diff > (Duration) TimeSpan.Zero, "Difference should be positive.");
            Assert(nowPlusX - diff == now, "Subtracting difference should yield original.");
            Assert(nowPlusX + diff == diff + nowPlusX, "Addition should be commutative.");
            DateTime dtLocalNowPlusX = nowPlusX.ToLocalDateTime();


            Console.WriteLine("Now do some converted to dt and ts arithmetic");
            Console.WriteLine("now converted to local datetime: [" + dtLocalNow.ToString("O") + "].");
            Console.WriteLine("nowplusx converted to local datetime: [" + dtLocalNowPlusX.ToString("O") + "].");
            TimeSpan tsDiff = dtLocalNowPlusX - dtLocalNow;
            Console.WriteLine("difference in milliseconds: [" + tsDiff.TotalMilliseconds.ToString("N6") + "].");
            Assert(tsDiff > TimeSpan.Zero, "ts diff > 0");
            Assert(dtLocalNowPlusX - tsDiff == dtLocalNow, "Difference between now plus x and x should be now.");
            
            bool needToDispose = false;
            List<ITimeStampThread<MonotonicStamp>> threads = null;
            try
            {
                int numThreads = 6;
                TimeSpan duration = TimeSpan.FromSeconds(30);
                TimeSpan sleepInterval = TimeSpan.FromSeconds(5);
                Console.WriteLine("Hello World!");
                string highPrecTs = TimeStampSource.IsHighPrecision ? " have " : " do not have ";
                Console.WriteLine($"We {highPrecTs} high precision monotonic timestamps.");
                Console.WriteLine($"Here is an example: [{MonotonicSource.Now:O}].");
                threads = CreateMonoThreads(numThreads, duration, sleepInterval);
                needToDispose = true;
                DateTime createdAllThreadsAt = MonotonicSource.Now;
                Console.WriteLine($"All {numThreads} threads started at [{createdAllThreadsAt:O}].");
                Console.WriteLine($"Should take slightly more than {duration.TotalSeconds:N} seconds.");
                var results = GenerateResults(threads);
                Console.WriteLine("Validating monotonicity thread...");
                foreach (var list in results.Values)
                {
                    ValidateMonotonicity(list);
                }
                Console.WriteLine("Intra-thread monotonicity validated.");
                needToDispose = false;
                DateTime gotResultsAt = MonotonicSource.Now;
                TimeSpan totalTime = gotResultsAt - createdAllThreadsAt;
                Console.WriteLine($"Results retrieved.  Time elapsed: {totalTime.TotalSeconds:F6} seconds.");
                Console.WriteLine("Processing results...");
                DateTime beginProcessResultsAt = MonotonicSource.Now;
                string textOfResults = ProcessResults(results, sleepInterval, stamp => stamp.ToLocalDateTime(),
                    (minuend, subtrahend) => (TimeSpan) (minuend - subtrahend));
                DateTime endProcessResultsAt = MonotonicSource.Now;
                TimeSpan elapsed = endProcessResultsAt - beginProcessResultsAt;
                Console.WriteLine($"It took {elapsed.TotalMilliseconds:F3} ms to process the results.");
                Console.WriteLine("Results:");
                Console.WriteLine(textOfResults);
                Console.WriteLine("Test COMPLETED OK.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test FAILED.  Exception: [{ex}].");
                Environment.Exit(-1);
            }
            finally
            {
                if (needToDispose)
                {
                    IEnumerable<ITimeStampThread> disposeUs = threads;
                    foreach (var item in disposeUs?.Reverse() ?? Enumerable.Empty<ITsGenThread>())
                    {
                        item.Dispose();
                    }
                }
            }

        }
        public static void ValidateMonotonicity(ImmutableList<DateTime> stamps)
        {
            if (stamps.Count > 1)
            {
                var priorStamp = stamps.First();
                int idx = 0;
                foreach (var stamp in stamps.Skip(1))
                {
                    ++idx;
                    if (stamp < priorStamp)
                    {
                        throw new InvalidOperationException(
                            $"Monotonicity validated stamp at idx {idx}.  Stamp at {idx} (value: {stamp}) is less than stamp at idx {idx - 1} (value: {priorStamp})!");
                    }
                    priorStamp = stamp;
                }
            }
        }
        public static void ValidateMonotonicity(ImmutableList<MonotonicStamp> stamps)
        {
            if (stamps.Count > 1)
            {
                var priorStamp = stamps.First();
                int idx = 0;
                foreach (var stamp in stamps.Skip(1))
                {
                    ++idx;
                    if (stamp < priorStamp)
                    {
                        throw new InvalidOperationException(
                            $"Monotonicity validated stamp at idx {idx}.  Stamp at {idx} (value: {stamp}) is less than stamp at idx {idx - 1} (value: {priorStamp})!");
                    }
                    priorStamp = stamp;
                }
            }
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]  //kinda the whole point
        static void Deny(bool predicate, string faultMsg)
        {
            if (predicate) throw new InvalidOperationException($"Test failed: {faultMsg}");
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")] //kinda the whole point
        static void Assert(bool predicate, string faultMsg)
        {
            if (!predicate)
                throw new InvalidOperationException($"Test failed: {faultMsg}");
        }

        static void TestHpTimestamps()
        {
            bool needToDispose = false;
            List<ITimeStampThread<DateTime>> threads = null;
            try
            {
                int numThreads = 6;
                TimeSpan duration = TimeSpan.FromSeconds(30);
                TimeSpan sleepInterval = TimeSpan.FromSeconds(5);
                Console.WriteLine("Hello World!");
                string highPrecTs = TimeStampSource.IsHighPrecision ? " have " : " do not have ";
                Console.WriteLine($"We {highPrecTs} high precision timestamps.");
                Console.WriteLine($"Here is an example: [{TimeStampSource.Now:O}].");
                threads = CreateHpThreads(numThreads, duration, sleepInterval);
                needToDispose = true;
                DateTime createdAllThreadsAt = TimeStampSource.Now;
                Console.WriteLine($"All {numThreads} threads started at [{createdAllThreadsAt:O}].");
                Console.WriteLine($"Should take slightly more than {duration.TotalSeconds:N} seconds.");
                var results = GenerateResults(threads);
                needToDispose = false;
                Console.WriteLine("Validating monotonicity thread...");
                foreach (var list in results.Values)
                {
                    ValidateMonotonicity(list);
                }
                Console.WriteLine("Intra-thread monotonicity validated.");
                DateTime gotResultsAt = TimeStampSource.Now;
                TimeSpan totalTime = gotResultsAt - createdAllThreadsAt;
                Console.WriteLine($"Results retrieved.  Time elapsed: {totalTime.TotalSeconds:F6} seconds.");
                Console.WriteLine("Processing results...");
                DateTime beginProcessResultsAt = TimeStampSource.Now;
                string textOfResults = ProcessResults(results, sleepInterval, stamp => stamp,(minuend, subtrahend) => minuend - subtrahend);
                DateTime endProcessResultsAt = TimeStampSource.Now;
                TimeSpan elapsed = endProcessResultsAt - beginProcessResultsAt;
                Console.WriteLine($"It took {elapsed.TotalMilliseconds:F3} ms to process the results.");
                Console.WriteLine("Results:");
                Console.WriteLine(textOfResults);
                Console.WriteLine("Test COMPLETED OK.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test FAILED.  Exception: [{ex}].");
                Environment.Exit(-1);
            }
            finally
            {
                if (needToDispose)
                {
                    IEnumerable<ITimeStampThread<DateTime>> thrdList = threads;
                    foreach (var item in thrdList?.Reverse() ?? Enumerable.Empty<ITimeStampThread<DateTime>>())
                    {
                        item.Dispose();
                    }
                }
            }
        }

        private static string ProcessResults<TStamp>(ImmutableDictionary<int, ImmutableList<TStamp>> results, TimeSpan sleepInterval, [JetBrains.Annotations.NotNull] Func<TStamp, DateTime> convertsToDateTime, [JetBrains.Annotations.NotNull] Func<TStamp, TStamp, TimeSpan> subtractStampsYieldingSpan) where TStamp : unmanaged, IEquatable<TStamp>, IComparable<TStamp>
        {
            TimeSpan epsilon = TimeSpan.FromMilliseconds(2);
            SortedDictionary<ResPair, Dictionary<int, int>>duplicateDictionary = new SortedDictionary<ResPair, Dictionary<int, int>>();
            StringBuilder sb = new StringBuilder();
            List<TimeSpan> shortestOfTheFilteredShort = new List<TimeSpan>();
            List<TimeSpan> longestOfTheFilteredLong = new List<TimeSpan>();
            List<TimeSpan> averageOfFilteredAverages = new List<TimeSpan>();
            foreach (var kvp in results)
            {
                sb.AppendLine($"Thread# {kvp.Key.ToString()} gathered {kvp.Value.Count} timestamps.");
                if (kvp.Value.Any())
                {
                    sb.AppendLine(
                        $"\tLast timestamp: {convertsToDateTime(kvp.Value.Last()):O}; First timestamp {convertsToDateTime(kvp.Value.First()):O}. Elapsed: " +
                        $"[{(convertsToDateTime(kvp.Value.Last()) - convertsToDateTime(kvp.Value.First())).TotalSeconds:F6}] seconds.");
                    AddValueToDuplDict(duplicateDictionary, kvp.Key, kvp.Value.Select(convertsToDateTime));
                    var statistics = GetStatistics(kvp.Value, subtractStampsYieldingSpan);
                    sb.AppendLine(
                        $"\tFor thread: [{kvp.Key.ToString()}], shortest consecutive interval: " +
                        $"[{statistics.ShortestConsecutiveDelay.TotalMilliseconds:F3}] ms; " +
                        "longest consecutive interval: " +
                        $"[{statistics.LongestConsecutiveDelay.TotalMilliseconds:F3}] ms; " +
                        "average consecutive interval: " +
                        $"[{statistics.AverageConsecutiveDelay.TotalMilliseconds:F3}] ms.");
                    
                    //this value is sorted ascending
                    var allConsecutiveSpans = statistics.AllSpans;
                    if (allConsecutiveSpans.Length > 1)
                    {
                        int indexOfFirstNotSleepAffectedSpan =
                            FindLastIndexOfSpanLessThanOrEqualTo(allConsecutiveSpans, sleepInterval - epsilon);
                        if (indexOfFirstNotSleepAffectedSpan > 0)
                        {
                            var span = allConsecutiveSpans.AsSpan();
                            span = span.Slice(0, indexOfFirstNotSleepAffectedSpan + 1);
                            if (span.Length > 1)
                            {
                                var filteredShortest = span[0];
                                var filteredLongest = span[^1];
                                TimeSpan averageFiltered = ComputeAverage(span);
                                sb.AppendLine("\tShowing FILTERED (for sleep interval) statistics:");
                                sb.AppendLine($"\tFor thread: [{kvp.Key.ToString()}], shortest consecutive interval: " +
                                              $"[{filteredShortest.TotalMilliseconds:F3}] ms; " +
                                              "longest consecutive interval: " +
                                              $"[{filteredLongest.TotalMilliseconds:F3}] ms; " +
                                              "average consecutive interval: " +
                                              $"[{averageFiltered.TotalMilliseconds:F3}] ms.");
                                shortestOfTheFilteredShort.Add(filteredShortest);
                                longestOfTheFilteredLong.Add(filteredLongest);
                                averageOfFilteredAverages.Add(averageFiltered);
                            }
                        }
                    }
                }
                sb.AppendLine();
            }

            var keys = duplicateDictionary.Keys.ToImmutableArray();
            foreach (var key in keys)
            {
                if (duplicateDictionary[key].Count <= 1)
                {
                    duplicateDictionary.Remove(key);
                }
            }

            if (duplicateDictionary.Any())
            {
                sb.AppendLine($"There are {duplicateDictionary.Count} duplicate timestamps.");
                foreach (var kvp in duplicateDictionary)
                {
                    sb.AppendLine($"\tThe timestamp: [{kvp.Key.TimeStamp:O}] appears {kvp.Value.Values.Sum()} times:");
                    foreach (var threadCountPair in kvp.Value)
                    {
                        sb.AppendLine(
                            $"\t\tIt appears {threadCountPair.Value} {TimeOrTimes(threadCountPair.Value)} in thread# [{threadCountPair.Key}].");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("There were no duplicate timestamps.");
            }

            if (shortestOfTheFilteredShort.Any())
            {
                shortestOfTheFilteredShort.Sort();
                sb.AppendLine(
                    $"The shortest of all filtered consecutive differences was: {shortestOfTheFilteredShort.First().TotalMilliseconds:F3} ms.");
            }
            if (longestOfTheFilteredLong.Any())
            {
                longestOfTheFilteredLong.Sort();
                sb.AppendLine(
                    $"The longest of all filtered consecutive differences was: {longestOfTheFilteredLong.Last().TotalMilliseconds:F3} ms.");
            }

            if (averageOfFilteredAverages.Any())
            {
                TimeSpan averageOfAverages = ComputeAverage(averageOfFilteredAverages.ToArray());
                sb.AppendLine(
                    $"The average of all average consecutive differences was {averageOfAverages.TotalMilliseconds:F3} ms.");
            }

            sb.AppendLine();
            return sb.ToString();

            string TimeOrTimes(int number) => number == 1 ? "time" : "times";

            int FindLastIndexOfSpanLessThanOrEqualTo(ImmutableArray<TimeSpan> sortedArrAscending, TimeSpan query)
            {
                for (int i = sortedArrAscending.Length - 1; i > -1; --i)
                {
                    TimeSpan span = sortedArrAscending[i];
                    if (span <= query)
                        return i;
                }
                return -1;
            }

            (ImmutableArray<TimeSpan> AllSpans, TimeSpan ShortestConsecutiveDelay, TimeSpan LongestConsecutiveDelay, TimeSpan AverageConsecutiveDelay)
                GetStatistics(ImmutableList<TStamp> times, Func<TStamp, TStamp, TimeSpan> sbtrYldSpn)
            {
                List<TimeSpan> spans;
                TimeSpan shortest, longest, average;
                switch (times.Count)
                {
                    case 1:
                    case 0:
                        return (ImmutableArray<TimeSpan>.Empty, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
                    default:
                        spans = ComputeDifferences(times, sbtrYldSpn);
                        break;
                }

                var allSpans = spans.ToImmutableArray().Sort();
                shortest = allSpans.First();
                longest = allSpans.Last();
                average = ComputeAverage(allSpans.AsSpan());
                return (allSpans, shortest, longest, average);
            }

            TimeSpan ComputeAverage(ReadOnlySpan<TimeSpan> spans)
            {
                TimeSpan sum = TimeSpan.Zero;
                switch (spans.Length)
                {
                    case 0:
                        return TimeSpan.Zero;
                    case 1:
                        return spans[0];
                    default:
                        foreach (var ts in spans)
                        {
                            sum += ts;
                        }

                        return sum / spans.Length;
                }
            }

            List<TimeSpan> ComputeDifferences(ImmutableList<TStamp> times, Func<TStamp, TStamp, TimeSpan> subtractStampsYieldSpan)
            {
                List<TimeSpan> spans = new List<TimeSpan>();
                Debug.Assert(times.Count > 1);
                for (int i = 0; i < times.Count - 1; ++i)
                {
                    TStamp earlier = times[i];
                    TStamp later = times[i + 1];
                    spans.Add(subtractStampsYieldSpan(later, earlier));
                }
                return spans;
            }

            void AddValueToDuplDict(SortedDictionary<ResPair, Dictionary<int, int>> res, int tn, IEnumerable<DateTime> dt)
            {
                foreach (var val in dt)
                {
                    ResPair pair = new ResPair(tn, val);
                    if (res.ContainsKey(pair))
                    {
                        var dict = res[pair];
                        if (dict.ContainsKey(tn))
                        {
                            ++dict[tn];
                        }
                        else
                        {
                            dict.Add(tn, 1);
                        }
                    }
                    else
                    {
                        res.Add(pair, new Dictionary<int, int>());
                        res[pair][tn] = 1;
                    }
                }
            }
            
        }



        static ImmutableDictionary<int, ImmutableList<TStamp>> GenerateResults<TStamp>([JetBrains.Annotations.NotNull] List<ITimeStampThread<TStamp>> threads) where TStamp : unmanaged, IEquatable<TStamp>, IComparable<TStamp>
        {
            var ret = ImmutableDictionary.CreateBuilder<int, ImmutableList<TStamp>>();
            IEnumerable<ITimeStampThread<TStamp>> toIterateBackwards = threads;
            foreach (var thread in toIterateBackwards.Reverse())
            {
                var list = thread.Join();
                ret.Add(thread.ThreadNumber, list);
                thread.Dispose();
            }
            return ret.ToImmutable();
        }

        static List<ITimeStampThread<MonotonicStamp>> CreateMonoThreads(int numThreads, TimeSpan duration, TimeSpan sleepInterval)
        {
            List<ITimeStampThread<MonotonicStamp>> ret = new List<ITimeStampThread<MonotonicStamp>>(numThreads);
            try
            {
                while (ret.Count < numThreads)
                {
                    ret.Add(MonoTsGeneratingThread.GenerateThread(duration, sleepInterval));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLineAsync(ex.ToString());
                IEnumerable<ITimeStampThread> threads = ret;
                foreach (var thread in threads.Reverse())
                {
                    try
                    {
                        thread.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        Console.Error.WriteLineAsync(ex2.ToString());
                    }
                }
                throw;
            }
            return ret;
        }
        static List<ITimeStampThread<DateTime>> CreateHpThreads(int numThreads, TimeSpan duration, TimeSpan sleepInterval)
        {
            List<ITimeStampThread<DateTime>> ret = new List<ITimeStampThread<DateTime>>(numThreads);
            try
            {
                while (ret.Count < numThreads)
                {
                    ret.Add(HpTsGeneratingThread.GenerateThread(duration, sleepInterval));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLineAsync(ex.ToString());
                IEnumerable<ITimeStampThread> threads = ret;
                foreach (var thread in threads.Reverse())
                {
                    try
                    {
                        thread.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        Console.Error.WriteLineAsync(ex2.ToString());
                    }
                }
                throw;
            }
            return ret;
        }

        private readonly struct ResPair : IComparable<ResPair>
        {
            public readonly DateTime TimeStamp;
            public readonly int ThreadNumber;

            public override string ToString() => $"Thrd#: {ThreadNumber.ToString()}; Timestamp: {TimeStamp:O}.";

            public ResPair(int num, DateTime ts)
            {
                TimeStamp = ts;
                ThreadNumber = num;
            }

            public int CompareTo(ResPair other) => TimeStamp.CompareTo(other.TimeStamp);
        }
    }

    internal interface ITimeStampThread : IDisposable
    {
        int ThreadNumber { get; }
        bool IsDisposed { get; }
        bool IsThreadActive { get; }
    }

    internal interface ITimeStampThread<TTimestamp> : ITimeStampThread
        where TTimestamp : unmanaged, IComparable<TTimestamp>, IEquatable<TTimestamp>
    {
        [JetBrains.Annotations.NotNull] ImmutableList<TTimestamp> Timestamps { get; }
        [JetBrains.Annotations.NotNull] ImmutableList<TTimestamp> Join();
    }

    internal interface ITsGenThread : ITimeStampThread<DateTime>
    {

    }

    internal interface IMonoTsGenThread : ITimeStampThread<MonotonicStamp>
    {

    }

    sealed class MonoTsGeneratingThread : IMonoTsGenThread
    {
        public static IMonoTsGenThread GenerateThread(TimeSpan duration, TimeSpan sleepInterval)
        {
            var ret = new MonoTsGeneratingThread(sleepInterval, duration);
            try
            {
                ret.Start();
                DateTime giveUp = MonotonicSource.Now + TimeSpan.FromMilliseconds(100);
                while (!ret.IsThreadActive && DateTime.Now <= giveUp)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }

                var status = ret._threadFlag.Status;
                if (status != ThreadStatus.Running && status != ThreadStatus.Terminated)
                {
                    throw new InvalidOperationException("Unable to confirm thread start.");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLineAsync(e.ToString());
                ret.Dispose();
                throw;
            }
            return ret;
        }

        public int ThreadNumber { get; }

        public ImmutableList<MonotonicStamp> Timestamps
        {
            get
            {
                var temp = _gathered;
                return temp ?? ImmutableList<MonotonicStamp>.Empty;
            }
        }

        public bool IsDisposed => _isDisposed.IsDisposed;

        public bool IsThreadActive
        {
            get
            {
                var temp = _threadFlag.Status;
                return (temp != ThreadStatus.Initialized && temp != ThreadStatus.Terminated);
            }
        }

        private MonoTsGeneratingThread(TimeSpan sleepInterval, TimeSpan duration)
        {
            _gathered = null;
            ThreadNumber = Interlocked.Increment(ref s_threadNum);
            _thread = new Thread(Loop)
            { Name = $"ThrdTsGen_{ThreadNumber.ToString()}", IsBackground = false, Priority = ThreadPriority.Normal };
            ValidateTimeSpans();
            _sleepInterval = sleepInterval;
            _duration = duration;
            Debug.Assert(_threadFlag.Status == ThreadStatus.Initialized);

            void ValidateTimeSpans()
            {
                if (sleepInterval <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(sleepInterval), sleepInterval, "Value must be positive.");
                if (duration <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(duration), duration, "Value must be positive.");
                if (sleepInterval * 5 > duration)
                    throw new ArgumentException(
                        $"Parameter {nameof(sleepInterval)} (value: {sleepInterval.TotalSeconds:F6} seconds) " +
                        $"multiplied by 5 (value * 5: {(sleepInterval * 5).TotalSeconds:F6)} seconds) " +
                        $"must be less than or equal to {nameof(duration)}  (value of duration: {duration.TotalSeconds:F6}). " +
                        @"It is not.");
            }
        }

        ~MonoTsGeneratingThread() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ImmutableList<MonotonicStamp> Join()
        {
            ImmutableList<MonotonicStamp> ret;
            if (_threadFlag.Status == ThreadStatus.Terminated)
            {
                var temp = _gathered;
                ret = temp ?? ImmutableList<MonotonicStamp>.Empty;
            }
            else
            {
                _thread.Join();
                while (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }
                var temp = _gathered;
                ret = temp ?? ImmutableList<MonotonicStamp>.Empty;
            }
            return ret;
        }

        private void Start()
        {
            _threadFlag.RequestStartOrThrow();
            _thread.Start(_cts.Token);
            while (_threadFlag.Status == ThreadStatus.StartRequested)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _isDisposed.TryDispose())
            {
                if (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    try
                    {
                        _cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLineAsync(ex.ToString());
                    }

                    DateTime giveUpAt = DateTime.Now + TimeSpan.FromSeconds(1);
                    while (_threadFlag.Status != ThreadStatus.Terminated && DateTime.Now <= giveUpAt)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                }
                DisposeLogExContinue(_cts);
            }
            else if (_isDisposed.TryDispose())
            {
                if (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    try
                    {
                        _cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLineAsync(ex.ToString());
                    }

                    DateTime giveUpAt = DateTime.Now + TimeSpan.FromSeconds(1);
                    while (_threadFlag.Status != ThreadStatus.Terminated && DateTime.Now <= giveUpAt)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                }
            }

            _isDisposed.TryDispose();

            void DisposeLogExContinue<TDisposable>(TDisposable disposable) where TDisposable : class, IDisposable
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLineAsync(e.ToString());
                }
            }
        }

        private void Loop(object cancellationToken)
        {
            try
            {
                if (cancellationToken is CancellationToken token)
                {
                    _threadFlag.SignalRunningOrThrow();
                    token.ThrowIfCancellationRequested();
                    TimeStampSource.Calibrate();
                    Execute(token);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Thread expected object of type {typeof(CancellationToken).Namespace}; it got {cancellationToken?.GetType().Name ?? "NULL REFERENCE"}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Thread# {ThreadNumber.ToString()} was cancelled.");
            }
            catch (Exception ex)
            {
                //test
                Console.Error.WriteLineAsync($"Thread# {ThreadNumber.ToString()} Faulted.  Exception: {ex}.");
            }
            finally
            {
                Interlocked.CompareExchange(ref _gathered, ImmutableList<MonotonicStamp>.Empty, null);
                _threadFlag.SignalTerminated();
            }
        }

        private void Execute(in CancellationToken token)
        {
            DateTime quitAfter = DateTime.Now + _duration;
            var builder = _gathered?.ToBuilder() ?? ImmutableList.CreateBuilder<MonotonicStamp>();
            try
            {
                while (DateTime.Now <= quitAfter)
                {
                    token.ThrowIfCancellationRequested();
                    for (int i = 0; i < 10; ++i)
                    {
                        builder.Add(MonotonicSource.StampNow);
                        Thread.SpinWait(2500);
                    }

                    SleepFor(_sleepInterval, in token);
                    token.ThrowIfCancellationRequested();
                    
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _gathered, builder.ToImmutable(), null);
            }
        }

        private void SleepFor(TimeSpan ts, in CancellationToken token)
        {
            DateTime wakeUpAfter = DateTime.Now + ts;
            while (DateTime.Now <= wakeUpAfter)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(ts);
            }
        }

        private DisposeFlag _isDisposed;
        private ThreadStatusFlag _threadFlag;
        [CanBeNull] private volatile ImmutableList<MonotonicStamp> _gathered;
        private readonly TimeSpan _sleepInterval;
        private readonly TimeSpan _duration;
        [JetBrains.Annotations.NotNull] private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static int s_threadNum;
    }


    sealed class HpTsGeneratingThread : ITsGenThread
    {
        public static ITsGenThread GenerateThread(TimeSpan duration, TimeSpan sleepInterval)
        {
            var ret = new HpTsGeneratingThread(sleepInterval, duration);
            try
            {
                ret.Start();
                DateTime giveUp = DateTime.Now + TimeSpan.FromMilliseconds(100);
                while (!ret.IsThreadActive && DateTime.Now <= giveUp)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }

                var status = ret._threadFlag.Status;
                if (status != ThreadStatus.Running && status != ThreadStatus.Terminated)
                {
                    throw new InvalidOperationException("Unable to confirm thread start.");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLineAsync(e.ToString());
                ret.Dispose();
                throw;
            }
            return ret;
        }

        public int ThreadNumber { get; }

        public ImmutableList<DateTime> Timestamps
        {
            get
            {
                var temp = _gathered;
                return temp ?? ImmutableList<DateTime>.Empty;
            }
        }

        public bool IsDisposed => _isDisposed.IsDisposed;

        public bool IsThreadActive
        {
            get
            {
                var temp = _threadFlag.Status;
                return (temp != ThreadStatus.Initialized && temp != ThreadStatus.Terminated);
            }
        }

        private HpTsGeneratingThread(TimeSpan sleepInterval, TimeSpan duration)
        {
            _gathered = null;
            ThreadNumber = Interlocked.Increment(ref s_threadNum);
            _thread = new Thread(Loop)
                {Name = $"ThrdTsGen_{ThreadNumber.ToString()}", IsBackground = false, Priority = ThreadPriority.Normal};
            ValidateTimeSpans();
            _sleepInterval = sleepInterval;
            _duration = duration;
            Debug.Assert(_threadFlag.Status == ThreadStatus.Initialized);

            void ValidateTimeSpans()
            {
                if (sleepInterval <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(sleepInterval), sleepInterval, "Value must be positive.");
                if (duration <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(duration), duration, "Value must be positive.");
                if (sleepInterval * 5 > duration)
                    throw new ArgumentException(
                        $"Parameter {nameof(sleepInterval)} (value: {sleepInterval.TotalSeconds:F6} seconds) " +
                        $"multiplied by 5 (value * 5: {(sleepInterval * 5).TotalSeconds:F6)} seconds) " +
                        $"must be less than or equal to {nameof(duration)}  (value of duration: {duration.TotalSeconds:F6}). " +
                        @"It is not.");
            }
        }

        ~HpTsGeneratingThread() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ImmutableList<DateTime> Join()
        {
            ImmutableList<DateTime> ret;
            if (_threadFlag.Status == ThreadStatus.Terminated)
            {
                var temp = _gathered;
                ret = temp ?? ImmutableList<DateTime>.Empty;
            }
            else
            {
                _thread.Join();
                while (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }
                var temp = _gathered;
                ret = temp ?? ImmutableList<DateTime>.Empty;
            }
            return ret;
        }

        private void Start()
        {
            _threadFlag.RequestStartOrThrow();
            _thread.Start(_cts.Token);
            while (_threadFlag.Status == ThreadStatus.StartRequested)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _isDisposed.TryDispose())
            {
                if (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    try
                    {
                        _cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLineAsync(ex.ToString());
                    }
                    
                    DateTime giveUpAt = DateTime.Now + TimeSpan.FromSeconds(1);
                    while (_threadFlag.Status != ThreadStatus.Terminated && DateTime.Now <= giveUpAt)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                }
                DisposeLogExContinue(_cts);
            }
            else if (_isDisposed.TryDispose())
            {
                if (_threadFlag.Status != ThreadStatus.Terminated)
                {
                    try
                    {
                        _cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLineAsync(ex.ToString());
                    }

                    DateTime giveUpAt = DateTime.Now + TimeSpan.FromSeconds(1);
                    while (_threadFlag.Status != ThreadStatus.Terminated && DateTime.Now <= giveUpAt)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                }
            }

            _isDisposed.TryDispose();

            void DisposeLogExContinue<TDisposable>(TDisposable disposable) where TDisposable : class, IDisposable
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLineAsync(e.ToString());
                }
            }
        }

        private void Loop(object cancellationToken)
        {
            try
            {
                if (cancellationToken is CancellationToken token)
                {
                    _threadFlag.SignalRunningOrThrow();
                    token.ThrowIfCancellationRequested();
                    TimeStampSource.Calibrate();
                    Execute(token);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Thread expected object of type {typeof(CancellationToken).Namespace}; it got {cancellationToken?.GetType().Name ?? "NULL REFERENCE"}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Thread# {ThreadNumber.ToString()} was cancelled.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLineAsync($"Thread# {ThreadNumber.ToString()} Faulted.  Exception: {ex}.");
            }
            finally
            {
                Interlocked.CompareExchange(ref _gathered, ImmutableList<DateTime>.Empty, null);
                _threadFlag.SignalTerminated();
            }
        }

        private void Execute(in CancellationToken token)
        {
            DateTime quitAfter = DateTime.Now + _duration;
            var builder = _gathered?.ToBuilder() ?? ImmutableList.CreateBuilder<DateTime>();
            try
            {
                while (DateTime.Now <= quitAfter)
                {
                    token.ThrowIfCancellationRequested();
                    for (int i = 0; i < 10; ++i)
                    {
                        builder.Add(TimeStampSource.Now);
                        Thread.SpinWait(2500);
                    }
                    token.ThrowIfCancellationRequested();
                    SleepFor(_sleepInterval, in token);
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _gathered, builder.ToImmutable(), null);
            }
        }

        private void SleepFor(TimeSpan ts, in CancellationToken token)
        {
            DateTime wakeUpAfter = DateTime.Now + ts;
            while (DateTime.Now <= wakeUpAfter)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(ts);
            }
        }

        private DisposeFlag _isDisposed;
        private ThreadStatusFlag _threadFlag;
        [CanBeNull] private volatile ImmutableList<DateTime> _gathered;
        private readonly TimeSpan _sleepInterval;
        private readonly TimeSpan _duration;
        [JetBrains.Annotations.NotNull] private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static int s_threadNum;
    }

    enum ThreadStatus
    {
        Initialized = 0,
        StartRequested,
        Running,
        Terminated,
    }

    internal struct DisposeFlag
    {
        public bool IsDisposed
        {
            get
            {
                int temp = _state;
                return (temp != NotDisposed);
            }
        }

        public bool TryDispose()
        {
            const int wantToBe = Disposed;
            const int needToBeNow = NotDisposed;
            return Interlocked.CompareExchange(ref _state, wantToBe, needToBeNow) == needToBeNow;
        }

        public override string ToString()
        {
            string disposedString = IsDisposed ? "[DISPOSED]" : "[NOT DISPOSED]";
            return $"{typeof(DisposeFlag).Name} state: {disposedString}.";
        }

        private volatile int _state;
        private const int NotDisposed = 0;
        private const int Disposed = 1;
    }
    internal struct ThreadStatusFlag
    {
        public ThreadStatus Status
        {
            get
            {
                ThreadStatus ret = (ThreadStatus) (_status);
                return ret;
            }
        }

        public bool TryRequestStart()
        {
            const int wantToBe = (int) ThreadStatus.StartRequested;
            const int needToBeNow = (int) ThreadStatus.Initialized;
            return Interlocked.CompareExchange(ref _status, wantToBe, needToBeNow) == needToBeNow;
        }

        public void RequestStartOrThrow()
        {
            if (!TryRequestStart())
                throw new InvalidOperationException("Not in valid state to request start.");
        }

        public bool TrySignalRunning()
        {
            const int wantToBe = (int)ThreadStatus.Running;
            const int needToBeNow = (int)ThreadStatus.StartRequested;
            return Interlocked.CompareExchange(ref _status, wantToBe, needToBeNow) == needToBeNow;
        }

        public void SignalRunningOrThrow()
        {
            if (!TrySignalRunning())
                throw new InvalidOperationException("Not in valid state to signal running.");
        }

        public bool SignalTerminated()
        {
            ThreadStatus oldValue = (ThreadStatus) (Interlocked.Exchange(ref _status, (int) ThreadStatus.Terminated));
            Debug.Assert(_status == (int) ThreadStatus.Terminated);
            return oldValue != ThreadStatus.Terminated;
        }

        public override string ToString()
        {
            string status = Status.ToString();
            return $"{typeof(ThreadStatusFlag).Name} state: [{status}].";
        }

        private volatile int _status;
    }
}

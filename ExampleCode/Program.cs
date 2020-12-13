using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using HpTimeStamps;
using JetBrains.Annotations;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using HpStamp = System.DateTime;
using HpStampSource = HpTimeStamps.TimeStampSource;
namespace ExampleTimestamps
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = MonotonicTimeStampUtil<MonotonicContext>;


    class Program
    {
        static void Main()
        {

            PrintHpInfo();
            Console.WriteLine();

            PrintMonotonicInfo();
            Console.WriteLine();

            Console.WriteLine("Here is default TimeStamp examples (configured to be monotonic).");
            DefaultStampExamples();
            Console.WriteLine("End default TimeStamp examples.");
            Console.WriteLine();

            Console.WriteLine("Here is wall clock example:");
            WallClockExample();
            Console.WriteLine("End wall clock example.");
            Console.WriteLine();

            Console.WriteLine("Here is monotonic clock example:");
            MonotonicClockExample();
            Console.WriteLine("End monotonic clock example.");
            Console.WriteLine();

            Console.WriteLine("Here is hp clock example:");
            HighPrecisionClockExample();
            Console.WriteLine("End hp clock example.");
            Console.WriteLine();


        }

        private static void PrintMonotonicInfo()
        {
            Console.WriteLine("Information about monotonic timestamps: ");
            try
            {
                bool tsAreHp = !MonoStampSource.StampContext.IsInvalid;
                Console.WriteLine($"This system {(tsAreHp ? "does" : "does not")} support monotonic timestamps.");
                if (!tsAreHp)
                {
                    //note that on implementations I have seen, if stopwatch is high res, it is monotonic. if not, then not.
                    Console.WriteLine("Monotonic stamps are not available because no clock with high res is present.");
                    return;
                }
                Console.WriteLine("TimeSpan ticks per second: {0:N0}", TimeSpan.TicksPerSecond);
                Console.WriteLine("Duration ticks per second: {0:N0}", Duration.TicksPerSecond);
                Console.WriteLine("Portable duration ticks per second: {0:N0}", PortableDuration.TicksPerSecond);
                Console.WriteLine("Local reference time: [{0:O}].", MonoStampSource.StampContext.LocalTimeBeginReference);
                Console.WriteLine("Utc reference time: [{0:O}].", MonoStampSource.StampContext.UtcDateTimeBeginReference);
                Console.WriteLine("Local utc offset: [{0:N3}] hours.", MonoStampSource.StampContext.UtcLocalTimeOffset.TotalHours);
                Console.WriteLine("Stopwatch tick equivalent to local time: [{0:N0}]",
                    MonoStampSource.StampContext.ReferenceTicks);
                Console.WriteLine("Easy conversion all around: [{0}]", MonoStampSource.StampContext.EasyConversionAllWays);
                Console.WriteLine("Easy conversions between stopwatch ticks and timespan ticks: [{0}]", MonoStampSource.StampContext.EasyConversionToAndFromTimespanTicks);
                Console.WriteLine("Easy conversions between stopwatch ticks and nanoseconds: [{0}]", MonoStampSource.StampContext.EasyConversionToAndFromNanoseconds);

                Console.WriteLine("Duration frequency: {0:N0} ticks per second.", Duration.TicksPerSecond);
                Console.WriteLine("Timespan frequency: {0:N0} ticks per second.", TimeSpan.TicksPerSecond);
                MonotonicStamp now = MonoStampSource.StampNow;
                Console.WriteLine("Initial local stamp: [{0:O}].", now.ToLocalDateTime());
                Console.WriteLine("Initial utc stamp: [{0:O}].", now.ToUtcDateTime());

                Console.WriteLine("Earliest instant representable as a monotonic timestamp in this process: [{0}].", MonotonicStamp.MinValue);
                Console.WriteLine("Same value converted to a UTC DateTime: [{0:O}].", MonotonicStamp.MinValue.ToUtcDateTime());
                Console.WriteLine("Same value converted to a Local DateTime: [{0:O}].", MonotonicStamp.MinValue.ToLocalDateTime());
                Console.WriteLine("Latest instant representable as a monotonic timestamp in this process: [{0}]", MonotonicStamp.MaxValue);
                Console.WriteLine("Same value converted to a UTC DateTime: [{0:O}].", MonotonicStamp.MaxValue.ToUtcDateTime());
                Console.WriteLine("Same value converted to a Local DateTime: [{0:O}].", MonotonicStamp.MaxValue.ToLocalDateTime());
                Console.WriteLine("Finished printing Monotonic Timestamp info.");
            }
            catch (UnsupportedStopwatchResolutionException ex)
            {
                Console.Error.WriteLine(
                    $"This system does not support monotonic timestamps because the resolution is smaller than this library supports.  Exception: [{ex}]");
            }
            catch (UnsupportedStopwatchException ex)
            {
                Console.Error.WriteLine($"This system's stopwatch is not supported by this library.  Reason: [{ex}]");
            }
            finally
            {
                Console.WriteLine("Done info about monotonic stamps.");
            }
        }

        private static void PrintHpInfo()
        {
            Console.WriteLine("Information about high precision timestamps: ");
            bool tsAreHp = HpStampSource.IsHighPrecision;
            Console.WriteLine($"This system {(tsAreHp ? "does" : "does not")} have high precision timestamps.");
            Console.WriteLine($"The frequency of the high precision timestamps is {Stopwatch.Frequency:N0} ticks per second.");
            Console.WriteLine("Finished printing High Precision Timestamp info.");
        }


        /// <summary>
        /// It is recommended that you stop using DateTime.Now and DateTime.UtcNow
        /// and use a static class like <see cref="TimeStampProvider"/> or similar arrangement.
        ///
        /// From the <see cref="TimeStampProvider"/> you can get DateTimes and monotonic stamps
        /// from a variety of clocks based on your needs.  You can also retrieve "default" datetimes
        /// and configure defaults based on wall clock, hp clock, monotonic clock or another clock depending
        /// on your use cases.  If the default turns out to be the wrong choice, it is then easy to reconfigure
        /// what clock the default <see cref="TimeStampProvider.Now"/> and <see cref="TimeStampProvider.UtcNow"/>
        /// work off of.  If you want the wall clock for sure, you use the wall clock via <see cref="TimeStampProvider.WallNow"/> or
        /// <see cref="TimeStampProvider.WallUtcNow"/>.  If you are doing a timing sensitive
        /// task with a deadline, use a <see cref="MonotonicStamp"/>.  If you are profiling a performance of a single thread
        /// over a period of a couple minutes, use <see cref="TimeStampProvider.HpNow"/> or <see cref="TimeStampProvider.HpUtcNow"/>.
        ///
        /// Using the system clock as the default source of DateTime stamps is a bad idea for the type of work I do ... but there may
        /// be cases and domains where it truly is the best.  Configure the <see cref="TimeStampProvider"/> or similar creation accordingly.
        /// If you change your mind about defaults, it will be easy to change and in one place.  
        /// </summary>
        private static void DefaultStampExamples()
        {
            //Because i've configured defaults to be monotonic the following two calls execute the same code (may well have different results because time elapses).
            DateTime utcStamp = TimeStampProvider.MonoUtcNow;
            DateTime utcDt = TimeStampProvider.UtcNow;
            DateTime asLocal = utcDt.ToLocalTime();
            Console.WriteLine(
                "In this example, these DateTimes come from a monotonic clock.  For that reason, they happen to be safe to use to measure when a task should end / timeout.  " +
                "Generally, however, for use as a deadline for a programming task (timeout time), it" +
                $"would be better to use {nameof(TimeStampProvider.MonoUtcNow)} or {nameof(TimeStampProvider.MonoLocalNow)} because these DateTimes are more obviously monotonic" +
                $".... or better still to work without potential need to convert timescales when checking if timeout happened by using {nameof(TimeStampProvider.MonoNow)}.");
            Console.WriteLine($"{nameof(utcStamp)}: [{utcStamp:O}].");
            Console.WriteLine($"{nameof(asLocal)}: [{asLocal:O}].");
            Console.WriteLine($"{nameof(utcDt)}: [{utcDt:O}].");
        }
       
        /// <summary>
        /// You should use the wall clock (i.e. DateTime now)
        /// when you need a calendar-based timestamp without needing to use
        /// the stamps later to determine how much time has elapsed between two events.
        ///
        /// DateTime (and TimeSpan) have a resolution of a tenth of a microsecond, but
        /// much of that is actually junk/garbage data.
        ///
        /// Advantages of WallClock (i.e. <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/>:
        ///        Most accurate (assuming machine has synchronization with time server) expression
        ///        of calendar date time for a thing.
        /// Disadvantages:
        ///        Not suitable for accurately measuring (particular on the scale relevant to a computer program)
        ///        time elapsed between events.  For example, system clock may be adjusted by user, admininstrator,
        ///        time server.  Daylights savings transitions may happen.  Leap seconds.
        ///
        ///        Most of the resolution available is not actually accurate: beyond several milliseconds (may vary based on environment),
        ///        values should not be relied on.
        /// </summary>
        static void WallClockExample()
        {
            //WallClock uses DateTime.Now and DateTime.UtcNow.  
            //The O format specifier indicates ISO-8601 roundtrippable format.
            //Because DateTime and TimeSpan are PRECISE to tenths of a microsecond,
            //and this is a roundtrip format (no loss of information), the entire fractional 
            //seconds resolution appears.  This is somewhat misleading because any value
            //finer than several milliseconds is garbage on most systems.
            DateTime localStamp = TimeStampProvider.WallNow;
            Console.WriteLine("An important file was created at {0:O}.", localStamp);

            DateTime utcStamp = TimeStampProvider.UtcNow;
            Console.WriteLine("Another important file was created at: {0:O}.", utcStamp);
        }

        /// <summary>
        /// Monotonic clocks are suitable for measuring the time elapsed between events and 
        /// use <see cref="System.Diagnostics.Stopwatch"/> (which uses a high resolution event timer
        /// if available).  On systems where it is available as a High Resolution clock
        /// (at least Linux and Windows right now) , it is also monotonic.  This means that the clock always
        /// moves forward ... unlike the wall clock, you can never take two readings and have the later reading
        /// yield an earlier time.  Unlike the wall clock, it is STEADY.  It is not subject to changes.  Using
        /// Stopwatch directly is often possible, but it does not give you a timestamp -- just elapsed time.
        ///
        /// In many scenarios (in my line of work) it is desirable to have a "dual use" timestamp: one that
        /// is suitable for displaying the date/time of events AND being safely usable to calculate elapsed time between events.
        /// 
        /// This stamp is designed to serve in this dual-use scenario (suitable to say what time something happened AND
        /// to use with other stamps to measure elapsed time).  Unlike the high precision clock provided, these stamps
        /// are calibrated once per process (across all threads) and thus can be used to establish at least a rough
        /// ordering of events across multiple threads.
        /// 
        /// Advantages: (note some advantages can also be disadvantages and vice versa)
        ///     1- It is safe to rely on this to measure intervals.
        ///     2- It is (usually) more accurate (vis-a-vis time elapsed between events), down to the precision and accuracy
        ///         of the hardware's high precision event time.
        ///     3- Unlike just using stopwatch you can still use these to log the time something happened AND use it to measure elapsed time
        ///     4- It is convertible to a DateTime.
        ///     5- It can safely be used to express a "timeout" deadline without fear that system clock will change, causing incorrect behavior.
        /// Disadvantages:
        ///     1- Because these (of necessity) are calibrated only once per process, over time there will likely be drift
        ///        between the system clock and this clock.  The system clock is usually synchronized and updated for daylights savings,
        ///        timezone changes, etc.  Thus, if (for example) it becomes daylight savings time while a process is running,
        ///        these stamps (when expressed as a DateTime or printed to ISO-8601) will not accurately reflect the actual time at which the event occurred.
        ///         a. Example.
        ///             -You start using these stamps at midnight on a day when transitioning to standard from daylight savings time.
        ///             -At what would normally be 3 am, the system clock moves back one hour for a "second" 2am.
        ///             -These timestamps will now not accurately reflect the local time at which something happened
        ///     2- These are not suitable for use across process boundaries or for serialization.  To keep the data in each value
        ///        small, calibration data is static (which correlates high precision performance counter ticks to a wall clock time once).  Thus
        ///        each value contains only an offset in stopwatch ticks from the reference time.  The reference time will be different for every process
        ///        and high precision event counter resolution varies by system.  Outside of current process, these values are meaningless
        ///     3- Some systems have very peculiar frequencies for the high performance event counter (as accessed via <see cref="System.Diagnostics.Stopwatch"/>).
        ///        On most Windows 10 systems I have tested this project on, the resolution of the <see cref="System.Diagnostics.Stopwatch"/> (and thus
        ///        this clock) is 10,000,000 ticks per second (i.e. 1 tick per tenth of a microsecond -- the same as <see cref="DateTime"/> and <see cref="TimeSpan"/>)
        ///        thus making conversions to and from DateTime and TimeSpan straightforward and without data loss.  On the Linux systems I have tested (Amazon Linux 2 and Ubuntu 20.04),
        ///        the resolution of stopwatch is 1,000,000,000.  This conversion is relatively straight forward and round: you lose hundredths and thousandths of microsecond resolution
        ///        when converting to DateTime and TimeSpan.  On other systems, however, the high precision event counter may have a resolution (though presumably a higher accuracy) LOWER than DateTime
        ///        and TimeSpan and have very non-round conversion factors (for example, on Windows Server hosted by Amazon AWS Work spaces, my test machine has a frequency of  2,441,366), which
        ///        complicated this project far beyond what I had originally anticipated.
        ///
        /// Note that is clock is unique in that it does not directly produce <see cref="DateTime"/> values.  Rather, it produces objects that express the time in terms of <see cref="System.Diagnostics.Stopwatch"/>
        /// ticks.  This keeps the actual size of individual values quite small.  They are fully convertible to DateTime but may lose precision based on the relative resolution of <see cref="DateTime"/> and <see cref="TimeSpan"/>
        /// and <see cref="System.Diagnostics.Stopwatch"/>.  Because which direction the loss will occur in and whether there will be loss due to rounding cannot be determined, conversions between this and datetime have to be explicit.
        /// These stamps provide <see cref="MonotonicStamp.ToLocalDateTime"/> and <see cref="MonotonicStamp.ToUtcDateTime"/> method for conversion as well as explicit casts.  Also, if not converting to DateTime before usage,
        /// this type has a companion analog to <see cref="TimeSpan"/> called <see cref="Duration"/>.  <see cref="Duration"/> is represented in terms of the same frequency as <see cref="MonotonicStamp"/> so arithmetic
        /// is not complex.  
        ///
        /// If you wish to serialize this data, you can:
        ///
        ///      1- convert <see cref="MonotonicStamp"/> to <see cref="DateTime"/> or <see cref="Duration"/> to <see cref="TimeSpan"/> first and save those values.  This may involve loss of precision depending
        ///         on frequencies.
        ///      2- convert <see cref="MonotonicStamp"/> to <see cref="PortableMonotonicStamp"/> or <see cref="Duration"/> to <see cref="PortableDuration"/>.  <see cref="PortableDuration"/> and <see cref="PortableMonotonicStamp"/>
        ///         (across ALL systems) have nanoseconds resolution -- unless your system's <see cref="Stopwatch"/> has a resolution finer than nanoseconds, there will not be lost precision .... EXCEPT perhaps some slight loss
        ///         if the <see cref="Stopwatch"/> frequency does not lend itself to clean conversion.  This is minimal in my experience.       
        /// 
        /// </summary>
        static void MonotonicClockExample()
        {
            MonotonicStamp startedStuffAt = TimeStampProvider.MonoNow;
            Duration doImportantStuffFor = Duration.FromMilliseconds(250);
            DoStuffUntil(startedStuffAt + doImportantStuffFor, CancellationToken.None);
            MonotonicStamp doneAt = TimeStampProvider.MonoNow;

            Console.WriteLine($"Started stuff at: {startedStuffAt}.");
            Console.WriteLine($"Did stuff for {doImportantStuffFor.TotalMilliseconds:N3}.");
            Console.WriteLine($"Ended at: {doneAt}.");

            //if you make your default stamp provider monotonic, it will convert to date time immediately. 
            //choose between wall / monotonic / high precision as "default" in TimeStampProvider based upon
            //what you think is the best default behavior.
            DateTime asDtUtc = startedStuffAt.ToUtcDateTime();
            DateTime asDtLocal = startedStuffAt.ToLocalDateTime();
            TimeSpan converted = (TimeSpan) doImportantStuffFor;
            DateTime doneAtUtc = doneAt.ToUtcDateTime();
            
            Console.WriteLine($"Stamp expressed as utc stamp: {asDtUtc:O}.");
            Console.WriteLine($"Stamp expressed as local stamp: {asDtLocal:O}.");
            Console.WriteLine($"Duration converted to TimeSpan: {converted.TotalMilliseconds:N3}.");
            Console.WriteLine($"Done at as utc timestamp: {doneAtUtc:O}.");

            PortableMonotonicStamp portableStampVersion = (PortableMonotonicStamp) startedStuffAt;
            PortableDuration portableDuration = (PortableDuration) doImportantStuffFor;
            PortableMonotonicStamp sum = portableStampVersion + portableDuration;
            Console.WriteLine("Portable duration: {0}.", portableDuration);
            Console.WriteLine($"Portable duration as microseconds: {portableDuration.TotalMicroseconds:N3}");
            Console.WriteLine("Portable stamp: {0}.", portableStampVersion);
            Console.WriteLine("Portable stamp as local: {0}.", portableStampVersion.ToLocalString());
            Console.WriteLine("sum: {0}.", sum);

            (string stampXml, PortableMonotonicStamp roundTripped) = Serialize(in sum);
            Console.WriteLine($"Xml for sum: {stampXml}.");
            Console.WriteLine($"Round tripped stamp: {roundTripped}.");

            (string durXml, PortableDuration roundTrippedDur) = Serialize(in portableDuration);
            Console.WriteLine($"Xml for portableDuration: {durXml}.");
            Console.WriteLine($"Round tripped value: {roundTrippedDur}");
            Console.WriteLine("Round tripped dur microseconds: {0:N3}.", roundTrippedDur.TotalMicroseconds);
        }

        /// <summary>
        /// Like the wall clock and unlike the monotonic clock, this source simply provides regular DateTimes but it used the <see cref="Stopwatch"/>
        /// rather than wall clock.  Like <see cref="MonotonicTimeStamp{TStampContext}"/>, there is an association of a stopwatch reading with a wall clock reading
        /// used to translate results from calls to <see cref="Stopwatch.GetTimestamp"/> to date times.  UNLIKE the monotonic clock, the calibration here is done on
        /// a PER THREAD basis.  Also, the calibration is for a limited duration .... after a time limit expires the stamps will be re-calibrated to the wall clock.
        ///
        /// Advantages:
        ///     1- Unlike monotonic clock, periodic recalibration reduces chance of significant drift between system clock and this clock.
        ///     2- On a per-thread basis within a given calibration period (assuming no system clock change) the stamps produced will be
        ///        more accurate (as far as <see cref="Stopwatch"/> and hardware allow) than stamps obtained with DateTime.Now.
        ///     3- They can thus be used for benchmarks or within smaller time windows to measure performance / benchmark as well
        ///        as serve as reasonably accurate timestamps (assuming one calibration period, no system clock change during it).
        /// Disadvantages:
        ///     1- Per thread.  Cannot be used to reliably relate relative occurrence of events on different threads.
        ///     2- Recalibration overhead (call <see cref="TimeStampProvider.CalibrateNow"/> before use to minimize possibility)
        ///     3- If a big adjustment to system clock happens after calibration but timestamps are read BEFORE next recalibration, stamps will not be accurate
        ///        vis-a-vis wall clock
        ///     4. If some stamps are gathered during calibration period A, then calibration period A ends and a big change is made to system clock, then recalibration happens
        ///        at period B, then more stamps are read, the stamps from period A cannot be used with stamps from period B to accurately measure elapsed time.
        /// </summary>
        static void HighPrecisionClockExample()
        {
            bool needsCalibration = TimeStampProvider.HpNeedsCalibration;
            Console.WriteLine($"needs calibration?: {needsCalibration}");
            if (needsCalibration)
            {
                Console.WriteLine("Calibrating.");
                TimeStampProvider.CalibrateNow();
                needsCalibration = TimeStampProvider.HpNeedsCalibration;
                Console.WriteLine($"needs calibration?: {needsCalibration}");
                Debug.Assert(!needsCalibration);
            }

            Console.WriteLine($"Elapsed time since last calibration: {TimeStampProvider.TimeSinceLastCalibration}.");
            
            var stamps = GetStamps(10);
            Console.WriteLine("Going to print timestamps: ");
            int stampCount = 0;
            foreach (var stamp in stamps)
            {
                Console.WriteLine("Hp stamp# {0:N}: [{1:O}].", ++stampCount, stamp);
            }
            Console.WriteLine("Done printing hp stamps.");

            TimeSpan differenceLastFirst = stamps.Last() - stamps.First();
            Console.WriteLine($"Time between first and last stamp: {differenceLastFirst}.");

            Console.WriteLine($"Final Elapsed time since calibration: {TimeStampProvider.TimeSinceLastCalibration}.");

            static ImmutableArray<HpStamp> GetStamps(int numStamps)
            {
                Debug.Assert(numStamps > 0);
                var arr = ImmutableArray.CreateBuilder<HpStamp>(numStamps);
                for (int i = 1; i <= numStamps; ++i)
                {
                    arr.Add(TimeStampProvider.HpNow);
                }

                return arr.MoveToImmutable();
            }
        }

        static void DoStuffUntil(MonotonicStamp deadline, CancellationToken token)
        {
            while (TimeStampProvider.MonoNow <= deadline)
            {
                token.ThrowIfCancellationRequested();
                Console.WriteLine("doing super important stuff......");
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        private static (string Xml, PortableMonotonicStamp RoundTripped) Serialize(in PortableMonotonicStamp pms)
        {
            var ser = new PortableStampSerializer();
            string xml = ser.SerializeToString(in pms);
            PortableMonotonicStamp roundTripped = ser.DeserializeFromString(xml);
            return (xml, roundTripped);
        }

        private static (string Xml, PortableDuration RoundTripped) Serialize(in PortableDuration pms)
        {
            var ser = new PortableDurationSerializer();
            string xml = ser.SerializeToString(in pms);
            PortableDuration roundTripped = ser.DeserializeFromString(xml);
            return (xml, roundTripped);
        }

        
    }


    internal readonly struct PortableStampSerializer 
    {
        public string SerializeToString(in PortableMonotonicStamp serializeMe)
        {
            using var output = new StringWriter();
            using var writer = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            TheDcSerializer.WriteObject(writer, serializeMe);
            return output.GetStringBuilder().ToString();
        }

    

        public PortableMonotonicStamp DeserializeFromString([NotNull] string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            using (Stream stream = new MemoryStream())
            {

                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                return (PortableMonotonicStamp)TheDcSerializer.ReadObject(stream);
            }
        }



        private static readonly DataContractSerializer TheDcSerializer = new DataContractSerializer(typeof(PortableMonotonicStamp));
    }

    internal readonly struct PortableDurationSerializer 
    {
        public string SerializeToString(in PortableDuration serializeMe)
        {
            using var output = new StringWriter();
            using var writer = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            TheDcSerializer.WriteObject(writer, serializeMe);
            return output.GetStringBuilder().ToString();
        }

     
        public PortableDuration DeserializeFromString([NotNull] string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            using (Stream stream = new MemoryStream())
            {

                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                return (PortableDuration)TheDcSerializer.ReadObject(stream);
            }
        }

        private static readonly DataContractSerializer TheDcSerializer = new DataContractSerializer(typeof(PortableDuration));
    }
}
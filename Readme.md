# High Precision Timestamps
## Copyright © 2020, CJM Screws, LLC

## A utility providing convenient and appropriate alternatives to DateTime.Now.  
  
It is well known that DateTime.Now is often used inappropriately.  For example, it may be used together with TimeSpan to produce a task's timeout point or subtracted from another DateTime to calculate a duration.  This can cause subtle bugs because DateTime is not monotonic: the system clock can change, making the result of the subtraction inaccurate -- potentially causing a premature timeout or an infinite loop.  Yet, DateTime is an incredibly convenient and widely used value type in .NET code and is especially useful when printed in ISO-8601 format (with the "O" format specifier).  

With the "O" specifier, you can resolution down to tenths of a microsecond, which is nice.  Until you learn that the resolution of the system clock is usually more coarse than several *milliseconds*, making the additional decimal places misleading garbage values. For calculating durations (time between events), it is better to use a high-resolution and monotonic clock like that provided by System.Diagnostics.Stopwatch: on most computers it is far more **accurate** than DateTime.Now even though, seemingly paradoxically, on a few systems, its *resolution* is lower than that of DateTime.  Also, unsurprisingly, Stopwatch does not provide values that correlate to times of day: while it is appropriate for calculating durations, it is inappropriate for timestamping against a readable date and time.  
  
This library provides timestamps (both as DateTime and as analogous value types it defines) that use the Stopwatch (and your system's high peformance event counter) as its clock, but returns values as DateTimes or an analog thereto so that these values can be used for a mixed purpose of timestamping and providing a meaningful way to calculate time elapsed between events.  

It provides **Monotonic** timestamps and **High Resolution** timestamps.  
  
### High Resolution Timestamps   

These timestamps are expressed as DateTime values and are derived from Stopwatch.  They are calibrated (correlating a reference tick value of the Stopwatch to a reference time value of the system clock) on a **per thread** basis and have a calibration window that expires.  These are suitable for logging times (in a way meaningful to humans) and can be used to measure the time elapsed between events *on a single thread* within **one calibration window**.  A calibration window by default lasts for fifteen minutes.  Eventually, there will be drift between the system clock and the Stopwatch, making recalibration necessary.  Nevertheless, for the resolution provided by Stopwatch, fifteen minutes should be a sufficient period for the intended use-case of these timestamps.  Also, you can always manually trigger a calibration.  
  
### Monotonic Timestamps  
  
These use the same source (Stopwatch) for their clock, but calibration happens exactly once per process and is the same across all threads.  Thus, you can accurately log the time of events across multiple threads and have meaningful data to compare when events happen.  Also, because calibration happens **once** these values are safe to use to calculate a timeout period, how long to perform a task, etc without the possibility that a change to the system clock can cause a bug.  Like the High Resolution Timestamps, their fractional seconds are meaningful on every system I have tested.  These are essentially dual-use values: they can be used to log timestamps and to calculate durations or time that your application should spend doing a task before quitting or timing out, etc.  The monotonic clock provided returns a value type provided by this library rather than DateTime, though the value type is conveniently convertible into a DateTime.  This choice was made because the Stopwatch's frequency can vary between systems and these stamps are intended to be used for calculating and measuring durations in addition to logging: it was desirable not to need to calculate a conversion to and from DateTime/TimeSpan scale when obtaining a stamp or performing a duration calculation.  For a similar reason, there is a Duration value type that is to the monotonic stamp what System.TimeSpan is to DateTime: a duration value with a matching frequency. 

### Example Code

An example code project (available at [ExampleCode](https://github.com/cpsusie/High-Precision-Time-Stamps/tree/master/ExampleCode)) is available and used to provide a tour of the functionality and its recommended use-cases.  All of the example code below can be found therein.  
  
I recommend that you do not use DateTime.Now directly in your code.  As explained, there are different clocks available in .NET and which clock is most suitable as a project default will vary and often will **not** be *DateTime.Now*.  Instead, if you use this library, I recommend that you create a static class that provides time stamps similar to the one shown here:  
  
```csharp
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using WallClock = System.DateTime;
using HpStamp = System.DateTime;
using HpStampSource = HpTimeStamps.TimeStampSource;
namespace ExampleTimestamps
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = MonotonicTimeStampUtil<MonotonicContext>;

    /// <summary>
    /// Static class used for obtaining DateTimes and other timestamps
    /// </summary>
    /// <remarks>In a non-demo implementation, <see cref="s_defaultProvider"/> should be WriteOnce (with thread-safe initializer)
    /// or readonly if you don't need to dynamically change default at runtime.
    /// </remarks>
    public static partial class TimeStampProvider
    {
        /// <summary>
        /// Indicates type of stamps retrieved by <see cref="Now"/> and <see cref="UtcNow"/>
        /// properties.
        /// </summary>
        public static DefaultStampType DefaultStampType
        {
            get
            {
                DefaultStampProvider provider = s_defaultProvider;
                switch (provider)
                {
                    default:
                    case null:
                        throw new InvalidOperationException("The provider is null or not of a recognized type.");
                    case WallClockProvider _:
                        return DefaultStampType.Wall;
                    case MonotonicClockProvider _:
                        return DefaultStampType.Monotonic;
                    case HighPrecisionClockProvider _:
                        return DefaultStampType.HighPrecision;
                }
            }
        }

        /// <summary>
        /// Use the default provider to get a timestamp expressed in local time
        /// </summary>
        public static DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_defaultProvider.DefaultNow;
        }

        /// <summary>
        /// Use the default provider to get a timestamp in UTC time
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_defaultProvider.DefaultUtcNow;
        }

        /// <summary>
        /// Get a monotonic timestamp
        /// </summary>
        public static MonotonicStamp MonoNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoStampSource.StampNow;
        }

        /// <summary>
        /// Get a monotonic timestamp expressed as a utc datetime
        /// </summary>
        public static DateTime MonoUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoNow.ToUtcDateTime();
        }

        /// <summary>
        /// Get a monotonic timestamp expressed as a local datetime
        /// </summary>
        public static DateTime MonoLocalNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoNow.ToLocalDateTime();
        }

        /// <summary>
        /// Get a high precision timestamp expressed as a local time
        /// </summary>
        public static HpStamp HpNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.Now;
        }

        /// <summary>
        /// Get a high precision timestamp expressed in UTC
        /// </summary>
        public static DateTime HpUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.UtcNow;
        }

        /// <summary>
        /// Get the wall clock time expressed as local time
        /// </summary>
        public static DateTime WallNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WallClock.Now;
        }

        /// <summary>
        /// Get the wall clock time expressed in utc
        /// </summary>
        public static HpStamp WallUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WallClock.UtcNow;
        }

        /// <summary>
        /// True if, on this thread, the high precision clock needs calibration.
        /// </summary>
        public static bool HpNeedsCalibration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.NeedsCalibration;
        }

        /// <summary>
        /// The amount of time elapsed since the High Precision clock's
        /// last calibration on this thread.
        /// </summary>
        public static TimeSpan TimeSinceLastCalibration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.TimeSinceCalibration;
        }

        /// <summary>
        /// Monotonic stamp context used by monotonic clock.  Contains
        /// information about frequencies, conversions, a reference time, etc.
        /// </summary>
        public static ref readonly MonotonicContext MonotonicContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MonoStampSource.StampContext;
        }

        #region Mutators
        /// <summary>
        /// Calibrate the high precision clock now
        /// </summary>
        public static void CalibrateNow() => HpStampSource.Calibrate();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use a monotonic clock as their source
        /// </summary>
        public static void UseMonotonicDefaultStamps() => s_defaultProvider = CreateMonotonicClock();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use a high precision clock as their source
        /// </summary>
        public static void UseHighPrecisionDefaultStamps() => s_defaultProvider = CreateHpClock();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use the wall clock as their source.
        /// </summary>
        public static void UseWallClockDefaultStamps() => s_defaultProvider = CreateWallClock();
        #endregion
        
        /// <summary>
        /// Adjust 
        /// </summary>
        static TimeStampProvider() => s_defaultProvider = new MonotonicClockProvider();


        [NotNull] private static DefaultStampProvider s_defaultProvider;

    }
```
As you can see, this provider allows you to retrieve TimeStamps from all of the clocks made available by this library as well as from DateTime.Now (called the "Wall Clock").  It also allows you to configure which clock will serve as the fault DateTime provider from its **Now** and **UtcNow** properties.  If you decide an individual clock is unsuitable as a project default, you can simply edit the provider to use a different clock for those two properties.  Optimally, programmers would usually avoid the defaults, and instead choose the Wall Clock when they want a DateTime synchronized with a time server, a monotonic stamp when calculating durations for a program to perform a particular piece of work or timeout, and a high precision stamp to measure the duration of events that happen on a single thread within a short period of each other.  Nevertheless, if your codebase is peppered with use of DateTime.Now plus TimeSpans to calculate durations, it may be best to choose the monotonic stamp to be your default and use an automated refactoring tool to change all references from DateTime.Now to **TimeStampProvider.MonoLocalNow** or **TimeStampProvider.MonoUtcNow** because using a non-monotonic clock for those purposes can cause somewhat unlikely but very difficult to diagnose bugs.  Once you start using the TimeStampProvider, you can always change the default clock or find individual places where, for example, the wall clock is actually the most appropriate.

Please note that I do not provide the TimeStampProvider as part of the library because it may not be suitable for everyone who wants to use the libary.  Instead, as mentioned, it is included in Example Code, and I recommend you adapt it or make something like it suitable to your needs.  

### Monotonic Stamp Usage Example
  
Note that the ExampleCode lists the use-cases pros and cons of each clock provided.  The following describes the monotonic timestamps and demonstrates their usage:  
  
```csharp
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
```
The output of that method (on my Windows 10 machine) is shown: 
> Here is monotonic clock example:  
doing super important stuff......  
doing super important stuff......  
doing super important stuff......  
doing super important stuff......  
doing super important stuff......  
Started stuff at: 2020-12-13T11:14:49.4562565-05:00.  
Did stuff for 250.000.  
Ended at: 2020-12-13T11:14:49.7125542-05:00.  
Stamp expressed as utc stamp: 2020-12-13T16:14:49.4562565Z.  
Stamp expressed as local stamp: 2020-12-13T11:14:49.4562565-05:00.  
Duration converted to TimeSpan: 250.000.  
Done at as utc timestamp: 2020-12-13T16:14:49.7125542Z.  
Portable duration: 250,000,000 nanoseconds.  
Portable duration as microseconds: 250,000.000  
Portable stamp: 2020-12-13T16:14:49.4562565Z.  
Portable stamp as local: 2020-12-13T11:14:49.4562565-05:00.  
sum: 2020-12-13T16:14:49.7062565Z.   

> Xml for sum: <PortableMonotonicStamp xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/HpTimeStamps">
>   <_dateTimeNanosecondOffsetFromMinValueUtc xmlns:d2p1="http://schemas.datacontract.org/2004/07/HpTimeStamps.BigMath">
>     <d2p1:_hi>3</d2p1:_hi>
>     <d2p1:_lo>8403222668577601652</d2p1:_lo>
>   </_dateTimeNanosecondOffsetFromMinValueUtc>
> </PortableMonotonicStamp>.  
> Round tripped stamp: 2020-12-13T16:14:49.7062565Z.
> Xml for portableDuration: <PortableDuration xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/HpTimeStamps">
>   <_ticks xmlns:d2p1="http://schemas.datacontract.org/2004/07/HpTimeStamps.BigMath">
>     <d2p1:_hi>0</d2p1:_hi>
>     <d2p1:_lo>250000000</d2p1:_lo>
>   </_ticks>
> </PortableDuration>.  

> Round tripped value: 250,000,000 nanoseconds  
> Round tripped dur microseconds: 250,000.000.  
> End monotonic clock example.  
  
Note that the results of conversions may vary based on how evenly the Stopwatch frequency is converted to and from nanoseconds and/or tenths of a microsecond (as used by **TimeSpan** and **DateTime**).  
  
### High Precision Usage Example:

The following code (again, found at [ExampleCode](https://github.com/cpsusie/High-Precision-Time-Stamps/tree/master/ExampleCode)) shows usage for the High Precision Timestamps and comments on their use cases and pros and cons:  
  
```csharp
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
```
When run, its output was: 
  
> Here is hp clock example:  
needs calibration?: True  
Calibrating.  
needs calibration?: False  
Elapsed time since last calibration: 00:00:00.0020710.  
Going to print timestamps:
Hp stamp# 1.00: [2020-12-13T11:14:49.8037592-05:00].  
Hp stamp# 2.00: [2020-12-13T11:14:49.8039701-05:00].  
Hp stamp# 3.00: [2020-12-13T11:14:49.8039735-05:00].  
Hp stamp# 4.00: [2020-12-13T11:14:49.8039739-05:00].  
Hp stamp# 5.00: [2020-12-13T11:14:49.8039743-05:00].  
Hp stamp# 6.00: [2020-12-13T11:14:49.8039746-05:00].  
Hp stamp# 7.00: [2020-12-13T11:14:49.8039755-05:00].  
Hp stamp# 8.00: [2020-12-13T11:14:49.8039759-05:00].  
Hp stamp# 9.00: [2020-12-13T11:14:49.8039763-05:00].  
Hp stamp# 10.00: [2020-12-13T11:14:49.8039766-05:00].  
Done printing hp stamps.  
Time between first and last stamp: 00:00:00.0002174.  
Final Elapsed time since calibration: 00:00:00.0141099.  
End hp clock example.  
  
### Status of Testing  / Project  

See release notes for version 0.1.1.0-beta for a fix involving serialization and deserialization of portable monotonic timestamps.

I have used this library to good effect in many projects.  It requires more unit tests and there will doubtless remain bugs.  It is, however, essentially feature complete and has been unit tested on four different systems:  
1. A Windows 10 System with a Stopwatch frequency of 10,000,000 ticks per second.  
2. An Ubuntu 20.04 system with a Stopwatch frequency of 1,000,000,000 ticks per second.  
3. An Amazon Linux 2 (based on CentOS) system with a Stopwatch frequency of 1,000,000,000 ticks per second.
4. An Amazon Workspaces Window Server (Windows 10 based Windows server) with (most vexingly) a stopwatch frequency of 2,441,366 ticks per second.  
  
I have decided to make this version a release despite knowing that there remain bugs (but having used the project extensively myself) and a need for additional unit tests because I believe it is in a useful, though imperfect, state.  Please inform me of any bugs found on the issues page or via email: I intend to fix bugs.  I consider this project more or less feature complete and do not imagine any additional extensive features being added by me.  If you would like to add features, I am happy to review any pull-request or issue.  
using System.Reactive.Linq;

namespace Stl.Time;

public static class ClockExt
{
    public static Task Delay(this IMomentClock clock, Moment dueAt, CancellationToken cancellationToken = default)
        => clock.Delay((dueAt - clock.Now).Positive(), cancellationToken);
    public static Task Delay(this IMomentClock clock, long dueInMilliseconds, CancellationToken cancellationToken = default)
    {
        if (dueInMilliseconds == Timeout.Infinite)
            return clock.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        if (dueInMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(dueInMilliseconds));
        return clock.Delay(TimeSpan.FromMilliseconds(dueInMilliseconds), cancellationToken);
    }

    public static IObservable<long> Timer(this IMomentClock clock, long delayInMilliseconds)
        => clock.Timer(TimeSpan.FromMilliseconds(delayInMilliseconds));
    public static IObservable<long> Timer(this IMomentClock clock, TimeSpan dueIn)
    {
        if (clock is SystemClock)
            return Observable.Timer(dueIn); // Perf. optimization
        return Observable.Create<long>(async observer => {
            var completed = false;
            try {
                await clock.Delay(dueIn).ConfigureAwait(false);
                observer.OnNext(0);
                completed = true;
                observer.OnCompleted();
            }
            catch (Exception e) {
                if (!completed)
                    observer.OnError(e);
            }
        });
    }
    public static IAsyncEnumerable<long> TimerAsync(this IMomentClock clock, long delayInMilliseconds)
        => clock.Timer(delayInMilliseconds).ToAsyncEnumerable();
    public static IAsyncEnumerable<long> TimerAsync(this IMomentClock clock, TimeSpan dueIn)
        => clock.Timer(dueIn).ToAsyncEnumerable();

    public static IObservable<long> Interval(this IMomentClock clock, long intervalInMilliseconds)
        => clock.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds));
    public static IObservable<long> Interval(this IMomentClock clock, TimeSpan interval)
        => clock is SystemClock
            ? Observable.Interval(interval) // Perf. optimization
            : clock.Interval(Intervals.Fixed(interval));
    public static IObservable<long> Interval(this IMomentClock clock, IEnumerable<TimeSpan> intervals)
    {
        var e = intervals.GetEnumerator();
        return Observable.Create<long>(async (observer, ct) => {
            var completed = false;
            try {
                var index = 0L;
                while (e.MoveNext()) {
                    var dueAt = clock.Now + e.Current;
                    await clock.Delay(dueAt, ct).SuppressCancellation().ConfigureAwait(false);
                    if (ct.IsCancellationRequested)
                        break;
                    observer.OnNext(index++);
                }
                completed = true;
                observer.OnCompleted();
            }
            catch (Exception e) {
                if (!completed)
                    observer.OnError(e);
            }
            finally {
                e.Dispose();
            }
        });
    }
    public static IAsyncEnumerable<long> IntervalAsync(this IMomentClock clock, long intervalInMilliseconds)
        => clock.Interval(intervalInMilliseconds).ToAsyncEnumerable();
    public static IAsyncEnumerable<long> IntervalAsync(this IMomentClock clock, TimeSpan interval)
        => clock.Interval(interval).ToAsyncEnumerable();
    public static IAsyncEnumerable<long> IntervalAsync(this IMomentClock clock, IEnumerable<TimeSpan> intervals)
        => clock.Interval(intervals).ToAsyncEnumerable();
}

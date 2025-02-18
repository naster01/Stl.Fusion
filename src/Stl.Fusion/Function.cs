using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion;

public interface IFunction : IHasServices
{
    ValueTask<IComputed> Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
    Task InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
}

public interface IFunction<in TIn, TOut> : IFunction
    where TIn : ComputedInput
{
    ValueTask<IComputed<TOut>> Invoke(TIn input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
    Task<TOut> InvokeAndStrip(TIn input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default);
}

public abstract class FunctionBase<TIn, TOut> : IFunction<TIn, TOut>
    where TIn : ComputedInput
{
    protected IAsyncLockSet<ComputedInput> Locks { get; }
    protected object Lock => Locks;
    protected readonly ILogger Log;
    protected readonly ILogger? DebugLog;

    public IServiceProvider Services { get; }

    protected FunctionBase(IServiceProvider services)
    {
        Services = services;
        Log = Services.LogFor(GetType());
        DebugLog = Log.IsLogging(LogLevel.Debug) ? Log : null;
        Locks = ComputedRegistry.Instance.GetLocksFor(this);
    }

    async ValueTask<IComputed> IFunction.Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken)
        => await Invoke((TIn) input, usedBy, context, cancellationToken).ConfigureAwait(false);

    public virtual async ValueTask<IComputed<TOut>> Invoke(TIn input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        // Read-Lock-RetryRead-Compute-Store pattern

        var result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result!;

        using var _ = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

        result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result!;

        result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
        result.UseNew(context, usedBy);
        return result;
    }

    Task IFunction.InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken)
        => InvokeAndStrip((TIn) input, usedBy, context, cancellationToken);

    public virtual Task<TOut> InvokeAndStrip(TIn input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        var result = GetExisting(input);
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(input, usedBy, context, cancellationToken);
    }

    protected async Task<TOut> TryRecompute(TIn input,
        IComputed? usedBy,
        ComputeContext context,
        CancellationToken cancellationToken = default)
    {
        using var _ = await Locks.Lock(input, cancellationToken).ConfigureAwait(false);

        var result = GetExisting(input);
        if (result.TryUseExisting(context, usedBy))
            return result.Strip(context);

        result = await Compute(input, result, cancellationToken).ConfigureAwait(false);
        var output = result.Output; // It can't be gone here b/c KeepAlive isn't called yet
        result.UseNew(context, usedBy);
        return output.Value;
    }

    protected IComputed<TOut>? GetExisting(TIn input)
    {
        var computed = ComputedRegistry.Instance.Get(input);
        return computed as IComputed<TIn, TOut>;
    }

    // Protected & private

    protected abstract ValueTask<IComputed<TOut>> Compute(
        TIn input, IComputed<TOut>? existing, CancellationToken cancellationToken);
}

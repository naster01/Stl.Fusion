using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public abstract class DbOperationCompletionNotifierBase<TDbContext, TOptions> : DbServiceBase<TDbContext>,
    IOperationCompletionListener, IDisposable, IAsyncDisposable, IHasWhenDisposed
    where TDbContext : DbContext
    where TOptions : DbOperationCompletionTrackingOptions, new()
{
    private volatile Task? _disposeTask = null;
    
    protected TOptions Options { get; init; }
    protected AgentInfo AgentInfo { get; }
    protected ConcurrentDictionary<Task, Unit> NotifyTasks { get; }
    protected IMomentClock CpuClock { get; }
    protected CancellationTokenSource StopTokenSource { get; }
    protected object Lock => StopTokenSource;

    public CancellationToken StopToken { get; }
    public Task? WhenDisposed => _disposeTask;

    protected DbOperationCompletionNotifierBase(TOptions options, IServiceProvider services)
        : base(services)
    {
        Options = options;
        AgentInfo = services.GetRequiredService<AgentInfo>();
        // CpuClock caching is needed coz otherwise an attempt to get them
        // in DisposeAsync will fail (service resolution fails during the disposal).
        CpuClock = services.Clocks().CpuClock;
        NotifyTasks = new ConcurrentDictionary<Task, Unit>();
        StopTokenSource = new CancellationTokenSource();
        StopToken = StopTokenSource.Token;
    }

    public void Dispose()
        => _ = DisposeAsync();

    public async ValueTask DisposeAsync()
    {
        Task disposeTask;
        lock (Lock) {
            if (_disposeTask == null) {
                StopTokenSource.CancelAndDisposeSilently();
                _disposeTask = DisposeAsyncCore();
            }
            disposeTask = _disposeTask;
        }
        await disposeTask.ConfigureAwait(false);
    }

    protected virtual async Task DisposeAsyncCore()
    {
        await CpuClock.Delay(Options.MaxCommitDuration, CancellationToken.None).ConfigureAwait(false);
        await Task.WhenAll(NotifyTasks.Keys).ConfigureAwait(false);
    }

    public bool IsReady() 
        => WhenDisposed == null;

    public Task OnOperationCompleted(IOperation operation)
    {
        if (!StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value)) // Only local commands require notification
            return Task.CompletedTask;
        
        var commandContext = CommandContext.Current;
        var tenant = Tenant.Default;
        if (commandContext != null) { // It's a command
            var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>();
            if (operationScope == null || !operationScope.IsUsed) // But it didn't change anything related to TDbContext
                return Task.CompletedTask;
            tenant = operationScope.Tenant;
        }
        
        var notifyChain = new AsyncChain($"Notify({tenant.Id})", _ => Notify(tenant))
            .Retry(Options.NotifyRetryDelays, Options.NotifyRetryCount, Clocks.CpuClock, Log);
        notifyChain.RunIsolated(CancellationToken.None);
        return Task.CompletedTask;
    }

    // Protected methods

    protected abstract Task Notify(Tenant tenant);
}

using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Client.Internal;

[BasePath("fusion/kvs")]
public interface ISandboxedKeyValueStoreClientDef
{
    [Post(nameof(Set))]
    Task Set([Body] SandboxedSetCommand command, CancellationToken cancellationToken = default);
    [Post(nameof(SetMany))]
    Task SetMany([Body] SandboxedSetCommand command, CancellationToken cancellationToken = default);
    [Post(nameof(Remove))]
    Task Remove([Body] SandboxedRemoveCommand command, CancellationToken cancellationToken = default);
    [Post(nameof(RemoveMany))]
    Task RemoveMany([Body] SandboxedRemoveCommand command, CancellationToken cancellationToken = default);

    [Get(nameof(Get))]
    Task<string?> Get(Session session, string key, CancellationToken cancellationToken = default);
    [Get(nameof(Count))]
    Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default);
    [Get(nameof(ListKeySuffixes))]
    Task<string[]> ListKeySuffixes(
        Session session,
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default);
}

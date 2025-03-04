namespace Stl.Fusion.Internal;

public interface IComputedImpl : IComputed
{
    void AddUsed(IComputedImpl used);
    void AddUsedBy(IComputedImpl usedBy); // Should be called only from AddUsedValue
    void RemoveUsedBy(IComputedImpl usedBy);

    void RenewTimeouts();
    void CancelTimeouts();

    bool IsTransientError(Exception error);
}

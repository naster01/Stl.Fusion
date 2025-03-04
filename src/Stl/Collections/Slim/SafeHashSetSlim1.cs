namespace Stl.Collections.Slim;

public struct SafeHashSetSlim1<T> : IHashSetSlim<T>
    where T : notnull
{
    private int _count;
    private T _item;
    private ImmutableHashSet<T>? _set;

    private bool HasSet {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set != null;
    }

    public int Count {
        get {
            if (HasSet) return _set!.Count;
            return _count;
        }
    }

    public bool Contains(T item)
    {
        if (HasSet) return _set!.Contains(item);
        if (_count >= 1 && EqualityComparer<T>.Default.Equals(_item, item)) return true;
        return false;
    }

    public bool Add(T item)
    {
        if (HasSet) {
            var set = _set!.Add(item);
            if (set == _set) return false;
            _set = set;
            return true;
        }

        // Item 1
        if (_count < 1) {
            _item = item;
            _count++;
            return true;
        }
        if (EqualityComparer<T>.Default.Equals(_item, item)) return true;

        _set = ImmutableHashSet<T>.Empty
            .Add(_item)
            .Add(item);
        _item = default!;
        return true;
    }

    public bool Remove(T item)
    {
        if (HasSet) {
            var set = _set!.Remove(item);
            if (set == _set) return false;
            _set = set;
            return true;
        }

        // Item 1
        if (_count < 1) return false;
        if (EqualityComparer<T>.Default.Equals(_item, item)) {
            _item = default!;
            _count--;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _set = null;
        _item = default!;
        _count = 0;
    }

    public IEnumerable<T> Items {
        get {
            if (HasSet) {
                foreach (var item in _set!)
                    yield return item;
                yield break;
            }
            if (_count < 1) yield break;
            yield return _item;
        }
    }

    public void Apply<TState>(TState state, Action<TState, T> action)
    {
        if (HasSet) {
            foreach (var item in _set!)
                action(state, item);
            return;
        }
        if (_count < 1) return;
        action(state, _item);
    }

    public void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                aggregator(ref state, item);
            return;
        }
        if (_count < 1) return;
        aggregator(ref state, _item);
    }

    public TState Aggregate<TState>(TState state, Func<TState, T, TState> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                state = aggregator(state, item);
            return state;
        }
        if (_count < 1) return state;
        state = aggregator(state, _item);
        return state;
    }

    public void CopyTo(Span<T> target)
    {
        var index = 0;
        if (HasSet) {
            foreach (var item in _set!)
                target[index++] = item;
            return;
        }
        if (_count < 1) return;
        target[index] = _item;
    }
}

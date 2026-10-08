using System.Buffers;

// A scratch buffer that is either stack memory or an array rented from a pool; Dispose returns a rented array.
readonly ref struct Lease<T> : IDisposable
{
    private readonly ArrayPool<T>? _pool;
    private readonly T[]? _rented;

    public Span<T> Span { get; }

    private Lease(ArrayPool<T>? pool, T[]? rented, Span<T> span)
    {
        _pool = pool;
        _rented = rented;
        Span = span;
    }

    public static Lease<T> FromStack(Span<T> span) => new(null, null, span);

    public static Lease<T> FromPool(ArrayPool<T> pool, int length)
    {
        var rented = pool.Rent(length);
        return new(pool, rented, rented);
    }

    public void Dispose()
    {
        if (_rented is not null) _pool!.Return(_rented);
    }
}

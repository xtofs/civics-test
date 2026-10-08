using System.Buffers;

/// <summary>
/// A lease represents a temporary buffer that can either be stack-allocated or rented from an array pool.
/// This allows for efficient temporary storage without frequent allocations.
/// When the lease is disposed, any rented array is returned to the pool.
/// </summary>
/// <typeparam name="T"></typeparam>
readonly ref struct Lease<T> : IDisposable
{
    private readonly ArrayPool<T>? _pool = ArrayPool<T>.Shared;

    private readonly T[]? _rented;

    public Span<T> Span { get; }

    /// <summary>
    /// the constructor that rents an array from the pool
    /// </summary>
    private Lease(int length)
    {
        _rented = _pool.Rent(length);
        Span = _rented;
    }

    /// <summary>
    /// the constructor that uses stack memory handed to it
    /// </summary>
    private Lease(Span<T> span)
    {
        _rented = null!;
        Span = span;
    }

    /// <summary>
    /// static factory method to create a Lease from stack memory
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static Lease<T> FromStack(Span<T> span)
    {
        return new Lease<T>(span);
    }

    /// <summary>
    /// static factory method to create a Lease from the array pool
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static Lease<T> FromPool(int length)
    {
        return new Lease<T>(length);
    }

    public void Dispose()
    {
        if (_rented is not null) _pool!.Return(_rented);
    }
}

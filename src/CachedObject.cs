using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Stevehead;

/// <summary>
/// Provides methodology to cache a value for a specified time and/or a number of accesses, and generates the value
/// again if either timedout or the maximum number of accesses has been reached.
/// </summary>
/// 
/// <typeparam name="T">The type of value to cache.</typeparam>
public sealed class CachedObject<T>
{
    // The function that supplies the value.
    private readonly object _supplier;

    // The allowed time to pass before timing out.
    private readonly TimeSpan _timeout;

    // The maximum number of accesses to cached value before resetting.
    private readonly long _maxCachedAccessCount;

    // The current value, if set.
    private T _value = default!;

    // Flag determining if value is set.
    private bool _isSet = false;

    // Time in which value was set.
    private DateTime _setAt;

    // Exception thrown during last call to supplier.
    private Exception? _exception = null;

    // The number of times the value has been accessed.
    private long _cachedAccessCount = 0L;

    // Internal Constructor
    private CachedObject(TimeSpan timeout, long maxCachedAccessCount, object supplier)
    {
        _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (maxCachedAccessCount < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCachedAccessCount));
        }

        _timeout = timeout;
        _maxCachedAccessCount = maxCachedAccessCount;
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="timeout">The amount of time to keep the value when generated.</param>
    /// <param name="maxCachedAccessCount">
    ///     The maximum number of access attempts allowed after value has been created.
    /// </param>
    /// <param name="supplier">The function that supplies the value.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="timeout"/> is negative.
    ///     -or -
    ///     <paramref name="maxCachedAccessCount"/> is negative.
    /// </exception>
    public CachedObject(TimeSpan timeout, long maxCachedAccessCount, Func<T> supplier) : this(timeout, maxCachedAccessCount, (object)supplier)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="timeout">The amount of time to keep the value when generated.</param>
    /// <param name="maxCachedAccessCount">
    ///     The maximum number of access attempts allowed after value has been created.
    /// </param>
    /// <param name="supplier">The function that supplies the value asynchronously.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="timeout"/> is negative.
    ///     -or -
    ///     <paramref name="maxCachedAccessCount"/> is negative.
    /// </exception>
    public CachedObject(TimeSpan timeout, long maxCachedAccessCount, Func<Task<T>> supplier) : this(timeout, maxCachedAccessCount, (object)supplier)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="timeout">The amount of time to keep the value when generated.</param>
    /// <param name="supplier">The function that supplies the value.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative.</exception>
    public CachedObject(TimeSpan timeout, Func<T> supplier) : this(timeout, long.MaxValue, (object)supplier)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="timeout">The amount of time to keep the value when generated.</param>
    /// <param name="supplier">The function that supplies the value asynchronously.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative.</exception>
    public CachedObject(TimeSpan timeout, Func<Task<T>> supplier) : this(timeout, long.MaxValue, (object)supplier)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="maxCachedAccessCount">
    ///     The maximum number of access attempts allowed after value has been created.
    /// </param>
    /// <param name="supplier">The function that supplies the value.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCachedAccessCount"/> is negative.</exception>
    public CachedObject(long maxCachedAccessCount, Func<T> supplier) : this(TimeSpan.MaxValue, maxCachedAccessCount, (object)supplier)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CachedObject{T}"/>.
    /// </summary>
    /// 
    /// <param name="maxCachedAccessCount">
    ///     The maximum number of access attempts allowed after value has been created.
    /// </param>
    /// <param name="supplier">The function that supplies the value asynchronously.</param>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCachedAccessCount"/> is negative.</exception>
    public CachedObject(long maxCachedAccessCount, Func<Task<T>> supplier) : this(TimeSpan.MaxValue, maxCachedAccessCount, (object)supplier)
    {
    }

    /// <summary>
    /// Indicates whether a value has been created.
    /// </summary>
    public bool IsValueCreated
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _isSet;
    }

    /// <summary>
    /// Indicates whether a value has been attempted to be created but faulted.
    /// </summary>
    public bool IsValueFaulted
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _isSet && _exception != null;
    }

    /// <summary>
    /// The total number of times a cached value can be accessed before being generated again.
    /// </summary>
    public long MaxCachedAccessCount => _maxCachedAccessCount;

    /// <summary>
    /// The remaining number of times the current value can be accessed before being generated again.
    /// </summary>
    public long RemainingCachedAccessCount
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            if (_isSet && _cachedAccessCount < _maxCachedAccessCount && DateTime.Now - _setAt < _timeout)
            {
                return _maxCachedAccessCount - _cachedAccessCount;
            }

            return 0L;
        }
    }

    /// <summary>
    /// The amount of time to keep the generated value.
    /// </summary>
    public TimeSpan Timeout => _timeout;

    /// <summary>
    /// The remaining amount of time before the value will have to be re-created.
    /// </summary>
    public TimeSpan TimeRemaining
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            if (_isSet && _cachedAccessCount < _maxCachedAccessCount)
            {
                var elapsedTime = DateTime.Now - _setAt;

                if (elapsedTime < _timeout)
                {
                    return _timeout - elapsedTime;
                }
            }

            return TimeSpan.Zero;
        }
    }

    private T GenerateValue()
    {
        if (_supplier is Func<T> syncSupplier)
        {
            return syncSupplier();
        }

        if (_supplier is Func<Task<T>> asyncSupplier)
        {
            return asyncSupplier().Result;
        }

        throw new InvalidOperationException("Should not get here.");
    }

    private async Task<T> GenerateValueAsync()
    {
        if (_supplier is Func<T> syncSupplier)
        {
            return syncSupplier();
        }

        if (_supplier is Func<Task<T>> asyncSupplier)
        {
            return await asyncSupplier();
        }

        throw new InvalidOperationException("Should not get here.");
    }

    private T GenerateAndSetValue()
    {
        try
        {
            _value = GenerateValue();
            _exception = null;
        }
        catch (Exception e)
        {
            _exception = e;
            throw;
        }
        finally
        {
            _setAt = DateTime.Now;
            _isSet = true;
            _cachedAccessCount = 0L;
        }

        return _value;
    }

    private async Task<T> GenerateAndSetValueAsync()
    {
        try
        {
            _value = await GenerateValueAsync();
            _exception = null;
        }
        catch (Exception e)
        {
            _exception = e;
            throw;
        }
        finally
        {
            _setAt = DateTime.Now;
            _isSet = true;
            _cachedAccessCount = 0L;
        }

        return _value;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Value
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            try
            {
                if (_isSet && _cachedAccessCount < _maxCachedAccessCount && DateTime.Now - _setAt < _timeout)
                {
                    if (_exception != null)
                    {
                        throw _exception;
                    }

                    return _value;
                }

                return GenerateAndSetValue();
            }
            finally
            {
                _cachedAccessCount++;
            }
        }
    }

    /// <summary>
    /// Gets the value asynchronously.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Task<T> GetValueAsync()
    {
        try
        {
            if (_isSet && _cachedAccessCount < _maxCachedAccessCount && DateTime.Now - _setAt < _timeout)
            {
                _cachedAccessCount++;

                if (_exception != null)
                {
                    throw _exception;
                }

                return Task.FromResult(_value);
            }

            return GenerateAndSetValueAsync();
        }
        finally
        {
            _cachedAccessCount++;
        }
    }

    /// <summary>
    /// Clears the state of this instance, forcing the next call to <see cref="Value"/> to re-create the value.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Reset()
    {
        _isSet = false;
        _setAt = default;
        _cachedAccessCount = 0L;
        _exception = null;
        _value = default!;
    }
}

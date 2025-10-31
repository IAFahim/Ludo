namespace Ludo;

public readonly struct Result<T, TError>
{
    private readonly bool _isOk;
    private readonly T _value;
    private readonly TError _error;

    private Result(bool isOk, T value, TError error)
    {
        _isOk = isOk;
        _value = value;
        _error = error;
    }

    public static Result<T, TError> Ok(T value) => new Result<T, TError>(true, value, default!);
    public static Result<T, TError> Err(TError error) => new Result<T, TError>(false, default!, error);

    public bool IsOk => _isOk;
    public bool IsErr => !_isOk;

    public T Unwrap()
    {
        if (!_isOk) throw new InvalidOperationException($"Called Unwrap on Err: {_error}");
        return _value;
    }

    public T UnwrapOr(T defaultValue) => _isOk ? _value : defaultValue;

    public T UnwrapOrElse(Func<TError, T> defaultFunc) => _isOk ? _value : defaultFunc(_error);

    public TError UnwrapErr()
    {
        if (_isOk) throw new InvalidOperationException("Called UnwrapErr on Ok");
        return _error;
    }

    public Result<U, TError> Map<U>(Func<T, U> mapper)
    {
        return _isOk ? Result<U, TError>.Ok(mapper(_value)) : Result<U, TError>.Err(_error);
    }

    public Result<T, F> MapErr<F>(Func<TError, F> mapper)
    {
        return _isOk ? Result<T, F>.Ok(_value) : Result<T, F>.Err(mapper(_error));
    }

    public Result<U, TError> AndThen<U>(Func<T, Result<U, TError>> binder)
    {
        return _isOk ? binder(_value) : Result<U, TError>.Err(_error);
    }

    public bool TryGetValue(out T value, out TError error)
    {
        value = _value;
        error = _error;
        return _isOk;
    }
}
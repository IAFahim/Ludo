using System;

namespace Ludo
{
    public readonly struct Result<T, TError>
    {
        private readonly bool _isOk;
        private readonly T? _value;
        private readonly TError? _error;

        private Result(bool isOk, T? value, TError? error)
        {
            _isOk = isOk;
            _value = value;
            _error = error;
        }

        public static Result<T, TError> Ok(T value) => new(true, value, default);
        public static Result<T, TError> Err(TError error) => new(false, default, error);

        public bool IsOk => _isOk;
        public bool IsErr => !_isOk;

        public T Unwrap() => _isOk ? _value! : throw new InvalidOperationException("Called Unwrap on Err result");
        public TError UnwrapErr() => !_isOk ? _error! : throw new InvalidOperationException("Called UnwrapErr on Ok result");

        public bool TryGetValue(out T value, out TError error)
        {
            if (_isOk)
            {
                value = _value!;
                error = default!;
                return true;
            }
            value = default!;
            error = _error!;
            return false;
        }

        public Result<TNew, TError> Map<TNew>(Func<T, TNew> mapper)
        {
            return _isOk ? Result<TNew, TError>.Ok(mapper(_value!)) : Result<TNew, TError>.Err(_error!);
        }

        public Result<TNew, TError> AndThen<TNew>(Func<T, Result<TNew, TError>> next)
        {
            return _isOk ? next(_value!) : Result<TNew, TError>.Err(_error!);
        }

        public Result<T, TNewError> MapErr<TNewError>(Func<TError, TNewError> mapper)
        {
            return _isOk ? Result<T, TNewError>.Ok(_value!) : Result<T, TNewError>.Err(mapper(_error!));
        }

        public void Tap(Action<T> action)
        {
            if (_isOk) action(_value!);
        }
    }

    public static class ResultExtensions
    {
        public static Result<Unit, TError> Ensure<TError>(bool condition, TError error)
        {
            return condition ? Result<Unit, TError>.Ok(Unit.Instance) : Result<Unit, TError>.Err(error);
        }
    }

    public readonly struct Unit
    {
        public static readonly Unit Instance = new();
    }
}

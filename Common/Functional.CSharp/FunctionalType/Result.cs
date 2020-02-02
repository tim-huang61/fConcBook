﻿using System;
using System.Threading.Tasks;

namespace Functional.CSharp.FuctionalType
{
    //Listing 10.8 The generic Result<T> type in C#
    public struct Result<T>
    {
        public T Ok { get; } // #A
        public Exception Error { get; } // #A

        public bool IsFailed => Error != null;
        public bool IsOk => !IsFailed;

        public Result(T ok) // #B
        {
            Ok = ok;
            Error = default;
        }

        public Result(Exception error) // #B
        {
            Error = error;
            Ok = default;
        }

        public R Match<R>(Func<T, R> okMap, Func<Exception, R> failureMap)
        {
            return IsOk ? okMap(Ok) : failureMap(Error); // #C
        }

        public void Match(Action<T> okAction, Action<Exception> errorAction)
        {
            if (IsOk) okAction(Ok);
            else errorAction(Error); // #C
        }

        public static implicit operator Result<T>(T ok)
        {
            return new Result<T>(ok); // #D
        }

        public static implicit operator Result<T>(Exception error)
        {
            return new Result<T>(error); // #D
        }

        public static implicit operator Result<T>(Result.Ok<T> ok)
        {
            return new Result<T>(ok.Value); //#D
        }

        public static implicit operator Result<T>(Result.Failure error)
        {
            return new Result<T>(error.Error); // #D
        }
    }

    public static class Result
    {
        public struct Ok<L>
        {
            internal L Value { get; }

            internal Ok(L value)
            {
                Value = value;
            }
        }

        public struct Failure
        {
            internal Exception Error { get; }

            internal Failure(Exception error)
            {
                Error = error;
            }
        }
    }

    //Listing 10.10 Task<Result<T>> helper functions for compositional semantic
    public static class ResultExtensions
    {
        public static Result.Ok<T> Ok<T>(T value)
        {
            return new Result.Ok<T>(value);
        }

        public static Result.Failure Failure(Exception error)
        {
            return new Result.Failure(error);
        }

        public static async Task<Result<T>> TryCatch<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public static Task<Result<T>> TryCatch<T>(Func<T> func)
        {
            return TryCatch(() => Task.FromResult(func()));
        }

        public static async Task<Result<R>> SelectMany<T, R>(this Task<Result<T>> resultTask,
            Func<T, Task<Result<R>>> func)
        {
            var result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok);
        }

        public static async Task<Result<R>> Select<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            var result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }

        public static async Task<Result<R>> Match<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> actionOk,
            Func<Exception, Task<R>> actionError)
        {
            var result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return await actionError(result.Error);
            return await actionOk(result.Ok);
        }

        public static async Task<Result<T>> ToResult<T>(this Task<Option<T>> optionTask)
            where T : class
        {
            var opt = await optionTask.ConfigureAwait(false);

            if (opt.IsSome()) return Ok(opt.Value);
            return new Exception();
        }

        public static async Task<Result<R>> OnSuccess<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }

        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Func<Task> func)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                await func().ConfigureAwait(false);
            return result;
        }

        public static async Task<Result<R>> Bind<T, R>(this Task<Result<T>> resultTask, Func<T, Task<Result<R>>> func)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }

        public static async Task<Result<R>> Map<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;

            return await TryCatch(() => func(result.Ok));
        }

        public static async Task<Result<R>> Match<T, R>(this Task<Result<T>> resultTask, Func<T, R> actionOk,
            Func<Exception, R> actionError)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return actionError(result.Error);
            return actionOk(result.Ok);
        }

        public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, Task<bool>> predicate,
            string errorMessage)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed || !await predicate(result.Ok).ConfigureAwait(false)) return result.Error;
            return result.Ok;
        }

        public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
        {
            var result = await resultTask.ConfigureAwait(false);

            if (result.IsOk)
                await action(result.Ok).ConfigureAwait(false);
            return result;
        }
    }
}
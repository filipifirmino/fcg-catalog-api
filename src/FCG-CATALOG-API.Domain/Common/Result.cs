using System;
using System.Collections.Generic;
using System.Linq;

namespace FCG_CATALOG_API.Domain.Common;

public class Result<T>
{
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool IsSuccess => !Errors.Any();

    private Result(T? value, IEnumerable<string> errors)
    {
        Value = value;
        Errors = errors.ToList();
    }

    public static Result<T> Success(T value) =>
        new(value, Array.Empty<string>());

    public static Result<T> Failure(IEnumerable<string> errors) =>
        new(default, errors);

    public static Result<T> Failure(string error) =>
        new(default, new[] { error });
}

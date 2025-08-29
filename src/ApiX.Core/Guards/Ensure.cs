#nullable enable

using System.Text.RegularExpressions;

namespace ApiX.Core.Guards;

/// <summary>
/// Represents the result of a guard or validation check.
/// Encapsulates whether the check succeeded (<see cref="IsValid"/>)
/// and an optional failure reason (<see cref="Reason"/>).
/// </summary>
public readonly struct CheckResult
{
    /// <summary>
    /// Gets a value indicating whether the check was successful.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the reason for failure, if the check was not successful; otherwise <c>null</c>.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckResult"/> struct.
    /// </summary>
    /// <param name="isValid">Indicates whether the check passed or failed.</param>
    /// <param name="reason">An optional failure reason; ignored if <paramref name="isValid"/> is <c>true</c>.</param>
    public CheckResult(bool isValid, string? reason = null)
    {
        IsValid = isValid;
        Reason = reason;
    }

    /// <summary>
    /// Creates a successful check result.
    /// </summary>
    /// <returns>A <see cref="CheckResult"/> with <see cref="IsValid"/> set to <c>true</c>.</returns>
    public static CheckResult Ok() => new(true, null);

    /// <summary>
    /// Creates a failed check result.
    /// </summary>
    /// <param name="reason">The reason the check failed.</param>
    /// <returns>A <see cref="CheckResult"/> with <see cref="IsValid"/> set to <c>false</c> and the given reason.</returns>
    public static CheckResult Fail(string? reason) => new(false, reason);

    /// <summary>
    /// Executes the specified action if the check failed.
    /// </summary>
    /// <param name="onInvalid">The action to invoke when <see cref="IsValid"/> is <c>false</c>.
    /// The <see cref="Reason"/> is passed to the action.</param>
    /// <returns><c>true</c> if the result was invalid (and the action was invoked); otherwise <c>false</c>.</returns>
    public bool IfInvalid(Action<string?> onInvalid)
    {
        if (!IsValid)
        {
            onInvalid(Reason);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Throws an exception if the check failed.
    /// </summary>
    /// <param name="exceptionFactory">A factory that creates the exception to throw,
    /// given the <see cref="Reason"/>.</param>
    /// <exception cref="Exception">The exception returned by <paramref name="exceptionFactory"/> if the result is invalid.</exception>
    public void ThrowIfInvalid(Func<string?, Exception> exceptionFactory)
    {
        if (!IsValid) throw exceptionFactory(Reason);
    }

    /// <summary>
    /// Returns a new <see cref="CheckResult"/> with the same validity,
    /// but with the failure reason replaced.
    /// </summary>
    /// <param name="reason">The replacement reason for failure.</param>
    /// <returns>
    /// The current instance if valid; otherwise, a failed <see cref="CheckResult"/>
    /// with the specified reason.
    /// </returns>
    public CheckResult WithReason(string? reason) => IsValid ? this : new CheckResult(false, reason);
}

/// <summary>
/// Provides a fluent guard around a value for ergonomic validation checks.
/// </summary>
/// <typeparam name="T">The type of the value being guarded.</typeparam>
/// <example>
/// Typical usage:
/// <code>
/// var guard = new Guard/<User/>(user);
/// var result = guard.NotNull("User must not be null");
///
/// // or fluently
/// Guard.That(user)
///     .NotNull("User required")
///     .IfInvalid(reason => Console.WriteLine(reason));
/// </code>
/// </example>
public readonly struct Guard<T>
{
    /// <summary>
    /// Gets the underlying value being checked by this guard.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Guard{T}"/> struct
    /// with the specified value to be validated.
    /// </summary>
    /// <param name="value">The entity value to be checked.</param>
    public Guard(T value) => Value = value;

    /// <summary>
    /// Evaluates a custom predicate against the guarded value.
    /// </summary>
    /// <param name="predicate">The condition to test.</param>
    /// <param name="reason">Optional failure reason if the predicate is not satisfied.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating whether the predicate was satisfied.
    /// </returns>
    public CheckResult Satisfies(Func<T, bool> predicate, string? reason = null) =>
        predicate(Value) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Predicate failed.");

    /// <summary>
    /// Returns the guarded value via an <c>out</c> parameter while preserving the guard instance.
    /// Useful for inline access in fluent chains.
    /// </summary>
    /// <param name="value">The guarded value.</param>
    /// <returns>The same <see cref="Guard{T}"/> instance for continued chaining.</returns>
    public Guard<T> That(out T value)
    {
        value = Value;
        return this;
    }

    /// <summary>
    /// Ensures the guarded value is not <c>null</c>.
    /// </summary>
    /// <param name="reason">Optional custom failure reason.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating success if the value is not <c>null</c>.
    /// </returns>
    public CheckResult NotNull(string? reason = null) =>
        Ensure.NotNull(Value, reason);

    /// <summary>
    /// Ensures the guarded value is <c>null</c>.
    /// </summary>
    /// <param name="reason">Optional custom failure reason.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating success if the value is <c>null</c>.
    /// </returns>
    public CheckResult Null(string? reason = null) =>
        Ensure.Null(Value, reason);

    /// <summary>
    /// Ensures the guarded value is not its type's default value.
    /// </summary>
    /// <param name="reason">Optional custom failure reason.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating success if the value is not the default for <typeparamref name="T"/>.
    /// </returns>
    public CheckResult NotDefault(string? reason = null) =>
        Ensure.NotDefault(Value, reason);

    /// <summary>
    /// Ensures the guarded value is equal to the specified other value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <param name="cmp">Optional equality comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="reason">Optional custom failure reason.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating success if the values are equal.
    /// </returns>
    public CheckResult EqualTo(T other, IEqualityComparer<T>? cmp = null, string? reason = null) =>
        Ensure.Equal(Value, other, cmp, reason);

    /// <summary>
    /// Ensures the guarded value is not equal to the specified other value.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <param name="cmp">Optional equality comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="reason">Optional custom failure reason.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> indicating success if the values are not equal.
    /// </returns>
    public CheckResult NotEqualTo(T other, IEqualityComparer<T>? cmp = null, string? reason = null) =>
        Ensure.NotEqual(Value, other, cmp, reason);
}

/// <summary>
/// Centralized guard/ensure checks. All methods are transport-agnostic and return CheckResult.
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Creates a <see cref="Guard{T}"/> instance for the specified value,
    /// enabling fluent validation checks.
    /// </summary>
    /// <typeparam name="T">The type of the value to be guarded.</typeparam>
    /// <param name="value">The value to wrap in a guard.</param>
    /// <returns>
    /// A <see cref="Guard{T}"/> that provides fluent validation methods
    /// such as <see cref="Guard{T}.NotNull(string?)"/> and <see cref="Guard{T}.EqualTo(T, IEqualityComparer{T}?, string?)"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// var user = new User { Email = "test@example.com" };
    ///
    /// var result = Ensure.That(user)
    ///                    .NotNull("User must not be null")
    ///                    .IfInvalid(reason => Console.WriteLine(reason));
    /// </code>
    /// </example>
    public static Guard<T> That<T>(T value) => new(value);

    // === Core null/default ===

    /// <summary>
    /// Ensures that the specified value is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value being checked.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Expected null."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the value is <c>null</c>,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult Null<T>(T? value, string? reason = null) =>
        value is null ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Expected null.");

    /// <summary>
    /// Ensures that the specified value is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value being checked.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value cannot be null."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the value is not <c>null</c>,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult NotNull<T>(T? value, string? reason = null) =>
        value is not null ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Value cannot be null.");

    /// <summary>
    /// Ensures that the specified value is not the default for its type.
    /// </summary>
    /// <typeparam name="T">The type of the value being checked.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value cannot be default."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the value is not the default value
    /// of <typeparamref name="T"/>; otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult NotDefault<T>(T value, string? reason = null)
    {
        if (EqualityComparer<T>.Default.Equals(value, default!))
            return CheckResult.Fail(reason ?? "Value cannot be default.");
        return CheckResult.Ok();
    }

    /// <summary>
    /// Ensures that the specified <see cref="Guid"/> is not <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Guid cannot be empty."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is not empty,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult GuidNotEmpty(Guid value, string? reason = null) =>
        value != Guid.Empty ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Guid cannot be empty.");

    // === String checks ===

    /// <summary>
    /// Ensures that the specified string is not <c>null</c> or empty (<c>""</c>).
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"String cannot be null or empty."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="s"/> is not <c>null</c> or empty,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult NotNullOrEmpty(string? s, string? reason = null) =>
        !string.IsNullOrEmpty(s) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "String cannot be null or empty.");

    /// <summary>
    /// Ensures that the specified string is not <c>null</c>, empty, or composed only of white-space characters.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"String cannot be null/whitespace."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="s"/> is not <c>null</c>, empty,
    /// or white-space, otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult NotNullOrWhiteSpace(string? s, string? reason = null) =>
        !string.IsNullOrWhiteSpace(s) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "String cannot be null/whitespace.");

    /// <summary>
    /// Ensures that the length of the specified string falls within the given inclusive range.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <param name="minInclusive">The minimum allowable string length (inclusive).</param>
    /// <param name="maxInclusive">The maximum allowable string length (inclusive).</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to a message indicating the required range.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the string length is between
    /// <paramref name="minInclusive"/> and <paramref name="maxInclusive"/> (inclusive).
    /// If <paramref name="s"/> is <c>null</c> or outside the range, the result is invalid.
    /// </returns>
    public static CheckResult LengthBetween(string? s, int minInclusive, int maxInclusive, string? reason = null)
    {
        if (s is null) return CheckResult.Fail(reason ?? "String cannot be null.");
        var len = s.Length;
        return (len >= minInclusive && len <= maxInclusive)
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? $"String length must be between {minInclusive} and {maxInclusive}.");
    }

    /// <summary>
    /// Ensures that the specified string matches the given regular expression pattern.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <param name="regex">The regular expression to test against.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"String did not match pattern."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="s"/> matches <paramref name="regex"/>,
    /// otherwise invalid with the provided or default reason. If <paramref name="s"/> is <c>null</c>,
    /// the result is invalid.
    /// </returns>
    public static CheckResult Matches(string? s, Regex regex, string? reason = null)
    {
        if (s is null) return CheckResult.Fail(reason ?? "String cannot be null.");
        return regex.IsMatch(s) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "String did not match pattern.");
    }

    // === Enumerable/collection checks ===

    /// <summary>
    /// Ensures that the specified sequence is <c>null</c> or contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Expected sequence to be empty."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the sequence is <c>null</c> or empty,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult Empty<T>(IEnumerable<T>? seq, string? reason = null) =>
        (seq is null || !seq.Any()) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Expected sequence to be empty.");

    /// <summary>
    /// Ensures that the specified sequence is not <c>null</c> and contains at least one element.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Sequence cannot be null or empty."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the sequence is not <c>null</c> and not empty,
    /// otherwise invalid with the provided or default reason.
    /// </returns>
    public static CheckResult NotEmpty<T>(IEnumerable<T>? seq, string? reason = null) =>
        (seq is not null && seq.Any()) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Sequence cannot be null or empty.");

    /// <summary>
    /// Ensures that the specified sequence contains at least one element.
    /// If a predicate is provided, ensures that at least one element matches it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="predicate">
    /// Optional condition to test elements against. If <c>null</c>, the check only tests for non-emptiness.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Sequence has no matching elements."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the sequence has at least one element
    /// (or at least one matching the predicate), otherwise invalid.
    /// </returns>
    public static CheckResult Any<T>(IEnumerable<T>? seq, Func<T, bool>? predicate = null, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        return (predicate is null ? seq.Any() : seq.Any(predicate))
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? "Sequence has no matching elements.");
    }

    /// <summary>
    /// Ensures that the specified sequence contains no elements.
    /// If a predicate is provided, ensures that no elements match it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="predicate">
    /// Optional condition to test elements against. If <c>null</c>, the check ensures the sequence is empty.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Sequence contained disallowed elements."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the sequence is empty
    /// (or no elements satisfy the predicate), otherwise invalid.
    /// </returns>
    public static CheckResult None<T>(IEnumerable<T>? seq, Func<T, bool>? predicate = null, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        return (predicate is null ? !seq.Any() : !seq.Any(predicate))
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? "Sequence contained disallowed elements.");
    }

    /// <summary>
    /// Ensures that all elements of the specified sequence satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="predicate">The condition that all elements must satisfy.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Not all elements satisfied predicate."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if all elements satisfy <paramref name="predicate"/>,
    /// otherwise invalid. If the sequence is <c>null</c>, the result is invalid.
    /// </returns>
    public static CheckResult All<T>(IEnumerable<T>? seq, Func<T, bool> predicate, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        return seq.All(predicate) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Not all elements satisfied predicate.");
    }

    /// <summary>
    /// Ensures that the sequence contains at least the specified minimum number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="minInclusive">The minimum required count (inclusive).</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to a message indicating the minimum required count.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the count of <paramref name="seq"/> is greater than
    /// or equal to <paramref name="minInclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult CountAtLeast<T>(IEnumerable<T>? seq, int minInclusive, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        var count = seq is ICollection<T> coll ? coll.Count : seq.Count();
        return count >= minInclusive ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Count must be ≥ {minInclusive}.");
    }

    /// <summary>
    /// Ensures that the sequence contains no more than the specified maximum number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="maxInclusive">The maximum allowed count (inclusive).</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to a message indicating the maximum allowed count.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the count of <paramref name="seq"/> is less than
    /// or equal to <paramref name="maxInclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult CountAtMost<T>(IEnumerable<T>? seq, int maxInclusive, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        var count = seq is ICollection<T> coll ? coll.Count : seq.Count();
        return count <= maxInclusive ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Count must be ≤ {maxInclusive}.");
    }

    /// <summary>
    /// Ensures that the sequence contains a number of elements within the specified inclusive range.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="minInclusive">The minimum allowable count (inclusive).</param>
    /// <param name="maxInclusive">The maximum allowable count (inclusive).</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to a message indicating the required range.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if the count of <paramref name="seq"/> is between
    /// <paramref name="minInclusive"/> and <paramref name="maxInclusive"/> (inclusive),
    /// otherwise invalid.
    /// </returns>
    public static CheckResult CountBetween<T>(IEnumerable<T>? seq, int minInclusive, int maxInclusive, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        var count = seq is ICollection<T> coll ? coll.Count : seq.Count();
        return (count >= minInclusive && count <= maxInclusive)
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? $"Count must be between {minInclusive} and {maxInclusive}.");
    }

    /// <summary>
    /// Ensures that all elements in the sequence are unique according to a key selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key used to determine uniqueness.</typeparam>
    /// <param name="seq">The sequence to validate.</param>
    /// <param name="keySelector">A function that extracts a comparison key from each element.</param>
    /// <param name="cmp">
    /// Optional equality comparer for comparing keys. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Sequence contains duplicate keys."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if all extracted keys are unique,
    /// otherwise invalid. If the sequence is <c>null</c>, the result is invalid.
    /// </returns>
    public static CheckResult UniqueBy<T, TKey>(IEnumerable<T>? seq, Func<T, TKey> keySelector, IEqualityComparer<TKey>? cmp = null, string? reason = null)
    {
        if (seq is null) return CheckResult.Fail(reason ?? "Sequence cannot be null.");
        cmp ??= EqualityComparer<TKey>.Default;
        var set = new HashSet<TKey>(cmp);
        foreach (var item in seq)
        {
            if (!set.Add(keySelector(item)))
                return CheckResult.Fail(reason ?? "Sequence contains duplicate keys.");
        }
        return CheckResult.Ok();
    }

    // === Equality / set membership ===

    /// <summary>
    /// Ensures that two values are equal using the specified equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of the values being compared.</typeparam>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="cmp">
    /// Optional equality comparer. If <c>null</c>, defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Values are not equal."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="a"/> and <paramref name="b"/>
    /// are equal according to the comparer; otherwise invalid.
    /// </returns>
    public static CheckResult Equal<T>(T a, T b, IEqualityComparer<T>? cmp = null, string? reason = null)
    {
        cmp ??= EqualityComparer<T>.Default;
        return cmp.Equals(a, b) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Values are not equal.");
    }

    /// <summary>
    /// Ensures that two values are not equal using the specified equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of the values being compared.</typeparam>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="cmp">
    /// Optional equality comparer. If <c>null</c>, defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Values must not be equal."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="a"/> and <paramref name="b"/>
    /// are not equal according to the comparer; otherwise invalid.
    /// </returns>
    public static CheckResult NotEqual<T>(T a, T b, IEqualityComparer<T>? cmp = null, string? reason = null)
    {
        cmp ??= EqualityComparer<T>.Default;
        return !cmp.Equals(a, b) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Values must not be equal.");
    }

    /// <summary>
    /// Ensures that a value is one of a specified set of allowed options.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="options">The set of allowed values.</param>
    /// <param name="cmp">
    /// Optional equality comparer. If <c>null</c>, defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value was not one of the allowed options."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is found in
    /// <paramref name="options"/> according to the comparer; otherwise invalid.
    /// </returns>
    public static CheckResult OneOf<T>(T value, IEnumerable<T> options, IEqualityComparer<T>? cmp = null, string? reason = null)
    {
        cmp ??= EqualityComparer<T>.Default;
        return options?.Contains(value, cmp) == true
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? "Value was not one of the allowed options.");
    }

    /// <summary>
    /// Ensures that a value is not part of a specified set of disallowed options.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="options">The set of disallowed values.</param>
    /// <param name="cmp">
    /// Optional equality comparer. If <c>null</c>, defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must not be one of the disallowed options."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is not found in
    /// <paramref name="options"/> according to the comparer; otherwise invalid.
    /// </returns>
    public static CheckResult NotOneOf<T>(T value, IEnumerable<T> options, IEqualityComparer<T>? cmp = null, string? reason = null)
    {
        cmp ??= EqualityComparer<T>.Default;
        return options?.Contains(value, cmp) != true
            ? CheckResult.Ok()
            : CheckResult.Fail(reason ?? "Value must not be one of the disallowed options.");
    }

    // === Comparable ranges ===

    /// <summary>
    /// Ensures that a value is strictly greater than the specified minimum.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="minExclusive">The exclusive lower bound.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must be &gt; {minExclusive}."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is greater than
    /// <paramref name="minExclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult GreaterThan<T>(T value, T minExclusive, string? reason = null) where T : IComparable<T> =>
        value.CompareTo(minExclusive) > 0 ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Value must be > {minExclusive}.");

    /// <summary>
    /// Ensures that a value is greater than or equal to the specified minimum.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="minInclusive">The inclusive lower bound.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must be ≥ {minInclusive}."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is greater than or equal to
    /// <paramref name="minInclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult GreaterOrEqual<T>(T value, T minInclusive, string? reason = null) where T : IComparable<T> =>
        value.CompareTo(minInclusive) >= 0 ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Value must be ≥ {minInclusive}.");

    /// <summary>
    /// Ensures that a value is strictly less than the specified maximum.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must be &lt; {maxExclusive}."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is less than
    /// <paramref name="maxExclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult LessThan<T>(T value, T maxExclusive, string? reason = null) where T : IComparable<T> =>
        value.CompareTo(maxExclusive) < 0 ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Value must be < {maxExclusive}.");

    /// <summary>
    /// Ensures that a value is less than or equal to the specified maximum.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="maxInclusive">The inclusive upper bound.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must be ≤ {maxInclusive}."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is less than or equal to
    /// <paramref name="maxInclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult LessOrEqual<T>(T value, T maxInclusive, string? reason = null) where T : IComparable<T> =>
        value.CompareTo(maxInclusive) <= 0 ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Value must be ≤ {maxInclusive}.");

    /// <summary>
    /// Ensures that a value lies within the specified inclusive range.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared. Must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="minInclusive">The inclusive lower bound.</param>
    /// <param name="maxInclusive">The inclusive upper bound.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Value must be between {minInclusive} and {maxInclusive}."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is greater than or equal to
    /// <paramref name="minInclusive"/> and less than or equal to <paramref name="maxInclusive"/>; otherwise invalid.
    /// </returns>
    public static CheckResult Between<T>(T value, T minInclusive, T maxInclusive, string? reason = null) where T : IComparable<T>
    {
        var ge = value.CompareTo(minInclusive) >= 0;
        var le = value.CompareTo(maxInclusive) <= 0;
        return (ge && le) ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Value must be between {minInclusive} and {maxInclusive}.");
    }

    // === Enum / misc ===

    /// <summary>
    /// Ensures that the specified enum value is defined within its enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
    /// <param name="value">The enum value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Enum value '{value}' is not defined."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="value"/> is a defined member
    /// of <typeparamref name="TEnum"/>; otherwise invalid.
    /// </returns>
    public static CheckResult EnumDefined<TEnum>(TEnum value, string? reason = null) where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? CheckResult.Ok() : CheckResult.Fail(reason ?? $"Enum value '{value}' is not defined.");

    /// <summary>
    /// Ensures that the specified string represents a valid URI of the given kind.
    /// </summary>
    /// <param name="s">The string to validate as a URI.</param>
    /// <param name="kind">
    /// The kind of URI to validate against (e.g., <see cref="UriKind.Absolute"/>).
    /// Defaults to <see cref="UriKind.Absolute"/>.
    /// </param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Invalid URI."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="s"/> can be parsed as a URI
    /// of the specified <paramref name="kind"/>; otherwise invalid.
    /// </returns>
    public static CheckResult ValidUri(string? s, UriKind kind = UriKind.Absolute, string? reason = null) =>
        Uri.TryCreate(s, kind, out _) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Invalid URI.");

    /// <summary>
    /// Ensures that the specified value satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value being checked.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The condition to test.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// Defaults to <c>"Predicate not satisfied."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="predicate"/> returns <c>true</c>
    /// for <paramref name="value"/>; otherwise invalid.
    /// </returns>
    public static CheckResult Satisfies<T>(T value, Func<T, bool> predicate, string? reason = null) =>
        predicate(value) ? CheckResult.Ok() : CheckResult.Fail(reason ?? "Predicate not satisfied.");


    // === Composition ===

    /// <summary>
    /// Combines multiple check results, returning valid only if all are valid.
    /// </summary>
    /// <param name="results">The set of check results to combine.</param>
    /// <returns>
    /// The first failed <see cref="CheckResult"/> if any checks fail; otherwise <see cref="CheckResult.Ok"/>.
    /// </returns>
    /// <remarks>
    /// This method short-circuits: the first invalid result is returned immediately.
    /// </remarks>
    public static CheckResult And(params CheckResult[] results)
    {
        foreach (var r in results)
            if (!r.IsValid)
                return r; // first failure short-circuits, preserve its reason
        return CheckResult.Ok();
    }

    /// <summary>
    /// Combines multiple check results, returning valid if any are valid.
    /// </summary>
    /// <param name="results">The set of check results to combine.</param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if at least one input is valid;
    /// otherwise invalid, with the reason from the last failed result.
    /// </returns>
    public static CheckResult Or(params CheckResult[] results)
    {
        string? lastReason = null;
        foreach (var r in results)
        {
            if (r.IsValid) return CheckResult.Ok();
            lastReason = r.Reason;
        }
        return CheckResult.Fail(lastReason ?? "No alternative succeeded.");
    }

    /// <summary>
    /// Negates the specified check result.
    /// </summary>
    /// <param name="result">The check result to negate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the negated result fails.
    /// Defaults to <c>"Negation failed (original was valid)."</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="result"/> is invalid,
    /// otherwise invalid.
    /// </returns>
    public static CheckResult Not(CheckResult result, string? reason = null) =>
        result.IsValid ? CheckResult.Fail(reason ?? "Negation failed (original was valid).")
                       : CheckResult.Ok();


    // === Handy overloads matching your original shapes ===

    /// <summary>
    /// Ensures that the specified value is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value to check.</typeparam>
    /// <param name="entity">The value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="entity"/> is <c>null</c>;
    /// otherwise invalid.
    /// </returns>
    public static CheckResult IsNull<T>(T? entity, string? reason = null) => Null(entity, reason);

    /// <summary>
    /// Ensures that the specified value is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value to check.</typeparam>
    /// <param name="entity">The value to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="entity"/> is not <c>null</c>;
    /// otherwise invalid.
    /// </returns>
    public static CheckResult IsNotNull<T>(T? entity, string? reason = null) => NotNull(entity, reason);

    /// <summary>
    /// Ensures that the specified collection is <c>null</c> or contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the collection element.</typeparam>
    /// <param name="entity">The collection to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="entity"/> is <c>null</c> or empty;
    /// otherwise invalid.
    /// </returns>
    public static CheckResult IsEmpty<T>(IEnumerable<T>? entity, string? reason = null) => Empty(entity, reason);

    /// <summary>
    /// Ensures that the specified collection is not <c>null</c> and contains at least one element.
    /// </summary>
    /// <typeparam name="T">The type of the collection element.</typeparam>
    /// <param name="entity">The collection to validate.</param>
    /// <param name="reason">
    /// Optional custom reason to return if the check fails.
    /// </param>
    /// <returns>
    /// A <see cref="CheckResult"/> that is valid if <paramref name="entity"/> is not <c>null</c>
    /// and contains at least one element; otherwise invalid.
    /// </returns>
    public static CheckResult IsNotEmpty<T>(IEnumerable<T>? entity, string? reason = null) => NotEmpty(entity, reason);
}

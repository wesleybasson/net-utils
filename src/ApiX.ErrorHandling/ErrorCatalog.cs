using System.Text.RegularExpressions;

namespace ApiX.ErrorHandling;

/// <summary>
/// Stable, enumerated error codes used across ApiX applications.  
/// Each value maps to a machine-readable string and a default human-readable message.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// Indicates that an attempt to create or register a user failed because the user already exists.
    /// </summary>
    Auth_UserExists,

    /// <summary>
    /// Indicates that authentication has failed for the provided credentials.
    /// </summary>
    Auth_LoginFailed,

    /// <summary>
    /// Indicates that a requested entity or resource could not be found.
    /// </summary>
    Entity_NotFound,

    /// <summary>
    /// Indicates that a requested entity could not be created.
    /// </summary>
    Entity_CreateFailed,

    /// <summary>
    /// Indicates that a requested entity could not be updated.
    /// </summary>
    Entity_UpdateFailed,

    /// <summary>
    /// Indicates that a requested entity could not be deleted.
    /// </summary>
    Entity_DeleteFailed,

    /// <summary>
    /// Indicates that one or more validation rules have failed.
    /// </summary>
    Validation_Failed,

    /// <summary>
    /// A generic error not otherwise categorized.
    /// </summary>
    Generic_Error
}

/// <summary>
/// Provides mapping utilities for <see cref="ErrorCode"/> values,
/// including stable string codes, default messages, and lightweight templating.
/// </summary>
public static class ErrorCatalog
{
    /// <summary>
    /// Converts an <see cref="ErrorCode"/> into its stable, machine-readable string representation.  
    /// Example: <see cref="ErrorCode.Entity_NotFound"/> → <c>"entity.not_found"</c>.
    /// </summary>
    /// <param name="c">The error code.</param>
    /// <returns>A machine-readable error string.</returns>
    public static string ToStringCode(this ErrorCode c) => c switch
    {
        ErrorCode.Auth_UserExists => "auth.user_exists",
        ErrorCode.Auth_LoginFailed => "auth.login_failed",
        ErrorCode.Entity_NotFound => "entity.not_found",
        ErrorCode.Entity_CreateFailed => "entity.create_failed",
        ErrorCode.Entity_UpdateFailed => "entity.update_failed",
        ErrorCode.Entity_DeleteFailed => "entity.delete_failed",
        ErrorCode.Validation_Failed => "validation.failed",
        _ => "generic.error"
    };

    /// <summary>
    /// Resolves the default human-readable message for an <see cref="ErrorCode"/>.  
    /// Example: <see cref="ErrorCode.Entity_NotFound"/> → <c>"The requested resource was not found."</c>.
    /// </summary>
    /// <param name="c">The error code.</param>
    /// <returns>A default error message suitable for end-users.</returns>
    public static string DefaultMessage(this ErrorCode c) => c switch
    {
        ErrorCode.Auth_UserExists => "A user with these details already exists.",
        ErrorCode.Auth_LoginFailed => "Authentication failed for the provided credentials.",
        ErrorCode.Entity_NotFound => "The requested resource was not found.",
        ErrorCode.Entity_CreateFailed => "The resource could not be created.",
        ErrorCode.Entity_UpdateFailed => "The resource could not be updated.",
        ErrorCode.Entity_DeleteFailed => "The resource could not be deleted.",
        ErrorCode.Validation_Failed => "One or more validation errors occurred.",
        _ => "An unexpected error has occurred."
    };

    /// <summary>
    /// Applies template parameters to a message string by replacing placeholders of the form <c>{Name}</c>.  
    /// Example: <c>"Entity {Id} not found."</c> + <c>new { Id = 42 }</c> → <c>"Entity 42 not found."</c>.
    /// </summary>
    /// <param name="template">The message template.</param>
    /// <param name="parameters">An anonymous object providing replacement values.</param>
    /// <returns>The formatted string with placeholders substituted.</returns>
    public static string Format(string template, object? parameters)
    {
        if (parameters is null) return template;
        var dict = parameters.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(parameters));

        return Regex.Replace(template, "{(.*?)}", m =>
        {
            var key = m.Groups[1].Value;
            return dict.TryGetValue(key, out var v) ? v?.ToString() ?? "" : m.Value;
        });
    }
}

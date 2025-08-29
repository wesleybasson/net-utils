
# ApiX.ErrorHandling

RFC7807 Problem Details + exception mapping + helpers for consistent, DI-friendly error handling in ApiX apps.

## Install

Add the project to your solution and reference it from your Web project.

```xml
<ItemGroup>
  <ProjectReference Include="..\ApiX.ErrorHandling\ApiX.ErrorHandling.csproj" />
</ItemGroup>
```

## Setup

```csharp
// Program.cs

builder.Services.AddApiXErrorHandling(options =>
{
    options.IncludeDetailsInDevelopment = true; // default true
    // Optional custom mappings
    // options.StatusMap[typeof(MyTransientNetworkException)] = StatusCodes.Status503ServiceUnavailable;
});

// Use the same handler for Dev/Prod so responses are consistent
app.UseExceptionHandler(ApiX.ErrorHandling.ApiXExceptionHandler.Handler);
```

## Throwing errors from domain/services

```csharp
throw new ApiXException(
    code: ErrorCode.Auth_UserExists.ToStringCode(),
    status: StatusCodes.Status409Conflict,
    message: ErrorCode.Auth_UserExists.DefaultMessage(),
    payload: new { email } // shown only in Development by default
);
```

## Returning errors/success from endpoints

```csharp
app.MapGet("/users/{id}", async (Guid id, IUserRepo repo) =>
{
    var user = await repo.Find(id);
    if (user is null)
        return ApiX.ErrorHandling.ApiXResults.Problem(
            ErrorCode.Entity_NotFound,
            StatusCodes.Status404NotFound,
            templateParameters: new { Id = id });

    return ApiX.ErrorHandling.ApiXResults.Ok(user);
});
```

## What this replaces

- Old `ErrorHandler` middleware → `ApiXExceptionHandler` (DI-friendly)
- `ErrorDictionary` & `GenericStrings` → `ErrorCatalog` + `ErrorCode`
- `ErrorResponse` → use `ApiXResults.Ok/Problem` or return your own DTOs
- Dozens of custom exceptions → a single `ApiXException` with code+status

## Notes

- Uses ASP.NET Core `ProblemDetailsFactory` for RFC7807 compliance.
- Emits `TraceId` and optional `Payload` (in Dev) for easier debugging.
- Ready to localize: swap `ErrorCatalog.DefaultMessage()` with `IStringLocalizer` later.

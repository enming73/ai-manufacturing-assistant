using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Manufacturing.Api;

public sealed class DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var databaseFailure = false;
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is DbException or TimeoutException) databaseFailure = true;
        if (!databaseFailure) return false;
        logger.LogError(exception, "Production data unavailable for {Path}", context.Request.Path);
        await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Production data is temporarily unavailable.").ExecuteAsync(context);
        return true;
    }
}

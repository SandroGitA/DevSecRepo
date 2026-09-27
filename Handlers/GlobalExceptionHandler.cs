using Microsoft.AspNetCore.Diagnostics;
using TestJob.Models;

namespace TestJob.Handlers;

public static class GlobalExceptionHandler
{
    public static void Configure(IApplicationBuilder handler)
    {
        handler.Run(async context =>
        {
            var exception = context.Features
                .Get<IExceptionHandlerFeature>()?.Error;

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            var result = new DevSecResult
            {
                IsError = 1,
                ErrorCode = "INTERNAL_ERROR",
                ErrorMessage = exception?.Message ?? "Неизвестная ошибка.",
                Url = string.Empty,
                DecryptedPlainText = string.Empty
            };

            await context.Response.WriteAsJsonAsync(
                result,
                options: new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                },
                cancellationToken: context.RequestAborted);
        });
    }
}

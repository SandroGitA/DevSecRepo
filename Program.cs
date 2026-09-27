using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TestJob.Handlers;
using TestJob.Models;
using TestJob.Services;
using TestJob.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Не удалось прочитать значение в JSON-запросе."
                        : error.ErrorMessage));

            return new BadRequestObjectResult(new DevSecResult
            {
                IsError = 1,
                ErrorCode = "INVALID_REQUEST",
                ErrorMessage = string.Join("; ", errors),
                Url = string.Empty,
                DecryptedPlainText = string.Empty
            });
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IDevSecService, DevSecService>();
builder.Services.AddScoped<IValidator<RequestDto>, RequestDtoValidator>();

var app = builder.Build();

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.UseExceptionHandler(GlobalExceptionHandler.Configure);

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "TestJob API v1");
    options.RoutePrefix = "api/swagger";
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

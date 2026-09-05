#pragma warning disable CA1506 // Composition roots necessarily connect the application's framework services.
using CombatSimulator.Api;
using CombatSimulator.Application;
using System.Text.Json;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiLimits.MaximumRequestBytes);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSimulationRateLimiting();
builder.Services.AddSingleton<BattleSimulationService>();

WebApplication app = builder.Build();
app.UseExceptionHandler();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseStatusCodePages(async context =>
{
    HttpResponse response = context.HttpContext.Response;
    if (response.HasStarted || response.ContentLength > 0)
        return;
    string title = response.StatusCode == StatusCodes.Status413PayloadTooLarge
        ? "Request body is too large"
        : "Request failed";
    await Results.Problem(
        statusCode: response.StatusCode,
        title: title)
        .ExecuteAsync(context.HttpContext).ConfigureAwait(false);
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.Run();

public partial class Program;
#pragma warning restore CA1506

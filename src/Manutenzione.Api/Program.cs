using System.Text.Json.Serialization;
using Manutenzione.Api.Data;
using Manutenzione.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Maintenance")
    ?? throw new InvalidOperationException("Stringa di connessione 'Maintenance' mancante: impostala in appsettings o in ConnectionStrings__Maintenance.");

builder.Services.AddDbContext<MaintenanceDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<MaintenanceDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>().Database.MigrateAsync();
    }
}

app.MapHealthChecks("/health");
app.MapAssets();
app.MapTechnicians();
app.MapWorkOrders();
app.MapPlans();

app.Run();

/// <summary>Esposto per i test di integrazione con WebApplicationFactory.</summary>
public partial class Program;

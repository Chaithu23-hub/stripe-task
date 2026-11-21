using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StripePayment.Application;
using StripePayment.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<StoreDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Store")));
builder.Services.AddScoped<IStore, SqlStore>();
builder.Services.AddSingleton<IPaymentGateway, StripeGateway>();
builder.Services.AddSingleton<StripeEvents>();
builder.Services.AddScoped<Checkout>();
builder.Services.AddScoped<ApplyPayment>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDataProtection().SetApplicationName("StripePayment.Api");
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Stripe Payment API"));
}

app.MapControllers();
app.Run();

public partial class Program { }

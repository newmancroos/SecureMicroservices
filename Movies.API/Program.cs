using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Movies.API.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MoviesContext>(options =>
    options.UseInMemoryDatabase("Movies"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5005";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });


builder.Services.AddAuthorization(options=>
{
    options.AddPolicy("ClientIdPolicy", policy => policy.RequireClaim("client_id", "movieClient"));
});
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options=> 
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

using var scope = app.Services.CreateScope();
    var service = scope.ServiceProvider;
    var movieContext = service.GetRequiredService<MoviesContext>();
    MovieContextSeed.SeedAsync(movieContext);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

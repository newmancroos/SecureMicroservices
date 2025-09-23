using CoreIdentity.Data;
using CoreIdentity.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication()
      .AddCookie() // IdentityConstants.ApplicationScheme);  //  If login I'll get error 500 becuase we are not configure JWT authentication, so if I switch Cookie true, I can login with cookie authentication.Here When I login cookie will be saved so I can continue.
      .AddBearerToken(IdentityConstants.BearerScheme);       // When login I'll get Access toekn and Refresh token.
                                                             // I can use refresh token with refresh      end-ppoint to get new access token


builder.Services.AddAuthorization();

builder.Services.AddDbContext<UserManagementContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("userManagementDb"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<UserManagementContext>()
    .AddApiEndpoints();  //This include all the EF core Identity end-points


var app = builder.Build();


if(app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if(!await roleManager.RoleExistsAsync(Roles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
    }
    if (!await roleManager.RoleExistsAsync(Roles.User))
    {
        await roleManager.CreateAsync(new IdentityRole(Roles.User));
    }

}

app.UseSwagger();
app.UseSwaggerUI();
RegsiterUser.MapEndPoint(app);  //All end point part of Entity Framewore core Identity, We can also define our end-point
app.UseHttpsRedirection();
app.MapIdentityApi<ApplicationUser>();   //This include all the EF core Identity end-points
app.Run();

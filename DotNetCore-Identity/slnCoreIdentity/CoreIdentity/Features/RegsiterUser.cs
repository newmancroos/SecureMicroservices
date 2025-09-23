using CoreIdentity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoreIdentity.Features;

public static class RegsiterUser
{

    public record Request (string Email, string Initials, string Password, bool EnableNotifications= false);

    public static void MapEndPoint(IEndpointRouteBuilder app)
    {

        ////register is the part of Identity end-point, but if we want to customize it we can do it here
        //app.MapPost("/register", async (Request request, 
        //    UserManagementContext dbContext,
        //    UserManager<ApplicationUser> userManager) =>
        //{

        //    //There many calls to database so we need to intriduce a transaction to avoid partial commit 
        //    //so We inject UserManagementContext and set the transaction

        //    using var transaction = dbContext.Database.BeginTransaction();
        //    var user = new ApplicationUser
        //    {
        //        UserName = request.Email,
        //        Email = request.Email,
        //        Initials = request.Initials,
        //        EnableNotificationa = request.EnableNotifications
        //    };
        //    IdentityResult result = await userManager.CreateAsync(user, request.Password);

        //    if (!result.Succeeded)
        //    {
        //        transaction.Rollback();
        //        return Results.BadRequest(result.Errors);
        //    }

        //    IdentityResult addToRoleResult =await userManager.AddToRoleAsync(user, Roles.Admin);


        //    if (!addToRoleResult.Succeeded)
        //    {
        //        transaction.Rollback();
        //        return Results.BadRequest(result.Errors);
        //    }
        //    transaction.Commit();
        //    return Results.Ok(user);
        //});

        app.MapGet("users/me", async (ClaimsPrincipal claims, UserManagementContext dbContext) =>
        {
            string userId = claims.Claims!.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)!.Value;


            return await dbContext.Users.FindAsync(userId);
        }).RequireAuthorization();
    }
}

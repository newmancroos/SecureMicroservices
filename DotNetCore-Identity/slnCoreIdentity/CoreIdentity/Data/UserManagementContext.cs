using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentity.Data;

public class UserManagementContext(DbContextOptions<UserManagementContext> options):IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); //We need this here because it has all the identity classes

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.EnableNotificationa).HasDefaultValue(true);
            entity.Property(e => e.Initials).HasMaxLength(5);
        });

        builder.HasDefaultSchema("Identity");
    }
}

public sealed class ApplicationUser : IdentityUser
{
    public bool EnableNotificationa { get; set; }
    public string? Initials { get; set; } = null!;

}
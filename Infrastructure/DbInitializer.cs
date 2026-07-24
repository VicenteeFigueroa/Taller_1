using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.Persistence;

namespace Shortly.Infrastructure;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, string adminPassword)
    {
        if (await db.Users.AnyAsync())
            return;

        var user = new User("admin@shortly.disc.cl", adminPassword);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Links.AddRange(
            new Link("https://learn.microsoft.com/aspnet/core", "aspnet-core1", user.Id),
            new Link("https://learn.microsoft.com/ef/core", "efcore-frame", user.Id),
            new Link("https://github.com", "github-repos", user.Id)
        );

        await db.SaveChangesAsync();
    }
}

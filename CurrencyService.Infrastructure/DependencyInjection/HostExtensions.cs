using CurrencyService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CurrencyService.Infrastructure.DependencyInjection;

public static class HostExtensions
{
    public static async Task MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

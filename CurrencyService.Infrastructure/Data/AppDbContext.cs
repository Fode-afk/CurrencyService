using CurrencyService.Application.Interfaces.Data;
using CurrencyService.Domain.Models;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace CurrencyService.Infrastructure.Data;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schemas.ExchangeRates);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<OutboxMessage>()
            .ToTable("OutboxMessages", Schemas.Messaging);

        modelBuilder.Entity<OutboxState>()
            .ToTable("OutboxState", Schemas.Messaging);

        modelBuilder.Entity<InboxState>()
            .ToTable("InboxState", Schemas.Messaging);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
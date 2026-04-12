using CurrencyService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using migApp.Shared.Domain.ValueObjects;

namespace CurrencyService.Infrastructure.ExchangeRates;

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(x => x.Currency);

        builder.Property(x => x.Currency)
            .HasConversion(
                currency => currency.Code,
                value => Currency.Create(value).Value)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();
    }
}

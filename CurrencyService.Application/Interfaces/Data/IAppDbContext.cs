using CurrencyService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyService.Application.Interfaces.Data;

public interface IAppDbContext
{
    DbSet<ExchangeRate> ExchangeRates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
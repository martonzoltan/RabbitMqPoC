using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Processor;

public class LocalContext : DbContext
{
    public DbSet<Hash> Hash { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"{{From Secrets}}");
    }
}
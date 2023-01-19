using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Context;

public class HashesDbContext : DbContext
{
    public HashesDbContext(DbContextOptions<HashesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Hash> Hash { get; set; }
}
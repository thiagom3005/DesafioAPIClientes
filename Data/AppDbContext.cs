using DesafioAPIClientes.Models;
using Microsoft.EntityFrameworkCore;

namespace DesafioAPIClientes.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(200);

            // Índice UNIQUE no Email
            entity.HasIndex(c => c.Email)
                .IsUnique();
        });
    }
}

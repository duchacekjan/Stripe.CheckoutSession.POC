using Microsoft.EntityFrameworkCore;

namespace POC.Api.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Event> Events { get; set; } = null!;
    public DbSet<Entities.Performance> Performances { get; set; } = null!;
    public DbSet<Entities.Seat> Seats { get; set; } = null!;
    public DbSet<Entities.Price> Prices { get; set; } = null!;
    
    public DbSet<Entities.Order> Orders { get; set; } = null!;
    public DbSet<Entities.OrderItem> OrderItems { get; set; } = null!;
    
    public DbSet<Entities.Payment> Payments { get; set; } = null!;
    
    public DbSet<Entities.CheckoutSession> CheckoutSessions { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
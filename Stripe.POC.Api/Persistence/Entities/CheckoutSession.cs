using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

[Table("CheckoutSessions")]
public class CheckoutSession : Entity
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
}

public class CheckoutSessionEntityConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(p => p.SessionId)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(p => p.ClientSecret)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasOne(o => o.Order)
            .WithOne(w => w.CheckoutSession)
            .HasForeignKey<CheckoutSession>(f => f.OrderId);
    }
}
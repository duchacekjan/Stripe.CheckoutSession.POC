using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class VoucherHistory : Entity
{
    public long VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class VoucherHistoryEntityConfiguration : IEntityTypeConfiguration<VoucherHistory>
{
    public void Configure(EntityTypeBuilder<VoucherHistory> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasOne(o => o.Voucher)
            .WithMany(w => w.History)
            .HasForeignKey(f => f.VoucherId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete history when voucher is deleted

        builder.HasOne(o => o.Order)
            .WithMany(m => m.Vouchers)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete history when order is deleted

        builder.ToTable("VoucherHistories");
    }
}
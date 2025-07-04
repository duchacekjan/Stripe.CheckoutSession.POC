using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POC.Api.Persistence.Entities;

public class Voucher : Entity
{
    public long SeatId { get; set; }
    public Seat Seat { get; set; } = null!;
    public decimal InitialAmount { get; set; }
    public decimal RemainingAmount => InitialAmount - History.Sum(s => s.Amount);
    public ICollection<VoucherHistory> History { get; set; } = new List<VoucherHistory>();

    public void RedeemAmount(long orderId, decimal amount)
    {
        if (RemainingAmount < amount)
        {
            throw new InvalidOperationException("Insufficient voucher balance.");
        }

        History.Add(new VoucherHistory
        {
            VoucherId = Id,
            OrderId = orderId,
            Amount = amount
        });
    }
}

public class VoucherEntityConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasOne(o => o.Seat)
            .WithOne()
            .HasForeignKey<Voucher>(f => f.SeatId)
            .OnDelete(DeleteBehavior.Cascade); // Assuming you want to delete voucher when seat is deleted

        builder.Ignore(i => i.RemainingAmount);
        builder.ToTable("Vouchers");
    }
}
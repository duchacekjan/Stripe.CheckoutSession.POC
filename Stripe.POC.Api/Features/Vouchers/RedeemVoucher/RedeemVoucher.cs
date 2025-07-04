using FastEndpoints;
using POC.Api.Common;

namespace POC.Api.Features.Vouchers.RedeemVoucher;

public static class RedeemVoucher
{
    public record Request(Guid BasketId, string VoucherCode);

    public class Endpoint(VouchersService voucherService) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Post("/redeem");
            Group<VouchersGroup>();
            Description(d => d
                .Produces<EmptyResponse>(StatusCodes.Status204NoContent)
                .WithName($"{nameof(Vouchers)}.{nameof(RedeemVoucher)}")
            );
            Summary(s =>
            {
                s.Summary = "Redeem voucher for basket";
                s.Responses[StatusCodes.Status200OK] = "Successfully redeemed voucher";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            await voucherService.RedeemVoucherAsync(req.BasketId, req.VoucherCode, ct);
            await SendNoContentAsync(ct);
        }
    }
}
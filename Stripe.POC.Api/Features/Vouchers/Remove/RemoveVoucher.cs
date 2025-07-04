using FastEndpoints;
using POC.Api.Common;

namespace POC.Api.Features.Vouchers.Remove;

public static class RemoveVoucher
{
    public record Request(Guid BasketId, string VoucherCode);
    
    public class Endpoint(VouchersService voucherService) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Delete("/remove");
            Group<VouchersGroup>();
            Description(d => d
                .Produces<EmptyResponse>(StatusCodes.Status204NoContent)
                .WithName($"{nameof(Vouchers)}.{nameof(RemoveVoucher)}")
            );
            Summary(s =>
            {
                s.Summary = "Remove voucher from basket";
                s.Responses[StatusCodes.Status204NoContent] = "Successfully removed voucher";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            await voucherService.RemoveVoucherAsync(req.BasketId, req.VoucherCode, ct);
            await SendNoContentAsync(ct);
        }
    }
}
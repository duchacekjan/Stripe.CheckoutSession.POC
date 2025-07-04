using FastEndpoints;
using POC.Api.Common;

namespace POC.Api.Features.Vouchers.Validate;

public static class ValidateVoucher
{
    public record Request(Guid BasketId, string VoucherCode);

    public record Response(bool IsValid, decimal? Discount, string? ErrorMessage = null);

    public class Endpoint(VouchersService voucherService) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Post("/validate");
            Group<VouchersGroup>();
            Description(d => d
                .Produces<Response>()
                .WithName($"{nameof(Vouchers)}.{nameof(ValidateVoucher)}")
            );
            Summary(s =>
            {
                s.Summary = "Validate voucher for basket";
                s.Responses[StatusCodes.Status200OK] = "Successfully validated voucher";
            });
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var result = await voucherService.ValidateVoucherAsync(req.BasketId, req.VoucherCode, ct);
            var response = new Response(result.IsValid, result.Discount, result.ErrorMessage);
            await SendOkAsync(response, ct);
        }
    }
}
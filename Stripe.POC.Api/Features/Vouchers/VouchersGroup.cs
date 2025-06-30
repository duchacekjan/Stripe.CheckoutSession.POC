using FastEndpoints;

namespace POC.Api.Features.Vouchers;

public sealed class VouchersGroup : Group
{
    public const string VoucherPrefix = "VOUCHER-";
    public VouchersGroup()
    {
        Configure("vouchers", c => { c.Description(d => { d.WithTags("Vouchers"); }); });
    }
}
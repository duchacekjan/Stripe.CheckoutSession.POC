using FastEndpoints;

namespace POC.Api.Features.Vouchers;

public sealed class VouchersGroup : Group
{
    public VouchersGroup()
    {
        Configure("vouchers", c => { c.Description(d => { d.WithTags("Vouchers"); }); });
    }
}
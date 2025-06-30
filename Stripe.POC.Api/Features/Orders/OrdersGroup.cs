using FastEndpoints;

namespace POC.Api.Features.Orders;

public sealed class OrdersGroup : Group
{
    public OrdersGroup()
    {
        Configure("orders", c => { c.Description(d => { d.WithTags("Orders"); }); });
    }
}
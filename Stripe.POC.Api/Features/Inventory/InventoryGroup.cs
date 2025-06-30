using FastEndpoints;

namespace POC.Api.Features.Inventory;

public sealed class InventoryGroup : Group
{
    public InventoryGroup()
    {
        Configure("inventory", c => { c.Description(d => { d.WithTags("Inventory"); }); });
    }
}
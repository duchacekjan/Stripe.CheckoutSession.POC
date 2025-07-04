using System.Text.Json.Serialization;

namespace POC.Api.Common;

public class Coupons : List<CouponItem>
{
    public override string ToString() => System.Text.Json.JsonSerializer.Serialize(this);
}

public class CouponItem
{
    [JsonPropertyName("coupon_data")]
    public CouponData Data { get; set; } = new();
}

public class CouponData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("amount_off")]
    public long AmountOff { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    
    public override string ToString() => System.Text.Json.JsonSerializer.Serialize(this);
}
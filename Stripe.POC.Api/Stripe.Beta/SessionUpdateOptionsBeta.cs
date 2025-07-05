using POC.Api.DTOs;
using Stripe.Checkout;

namespace POC.Api.Stripe.Beta;

public class SessionUpdateOptionsBeta : SessionUpdateOptions
{
    public void AddVouchers(List<VoucherDTO> vouchers)
    {
        var voucherTotal = vouchers.Sum(v => v.Amount) * 100;
        if (vouchers.Count == 0 || voucherTotal <= 0)
        {
            AddExtraParam("discounts", null);
            return;
        }

        AddExtraParam("discounts[0][coupon_data][name]", "Customer voucher discount sum");
        AddExtraParam("discounts[0][coupon_data][amount_off]", (long)voucherTotal);
        AddExtraParam("discounts[0][coupon_data][currency]", "gbp");

        // TODO Not supported in Stripe yet
        // for (var i = 0; i < vouchers.Count; i++)
        // {
        //     var voucher = vouchers[i];
        //     AddExtraParam($"discounts[{i}][coupon_data][name]", voucher.Code[..6]);
        //     AddExtraParam($"discounts[{i}][coupon_data][amount_off]", (long)(voucher.Amount * 100));
        //     AddExtraParam($"discounts[{i}][coupon_data][currency]", "gbp");
        // }
    }
}
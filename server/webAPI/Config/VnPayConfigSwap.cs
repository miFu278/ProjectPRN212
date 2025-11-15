namespace webAPI.Config
{
    public static class VnPayConfigSwap
    {
        // Populated from appsettings.json at startup. Defaults provided.
        public static string vnp_PayUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public static string vnp_TmnCode { get; set; } = "N5V11ZKB";
        public static string vnp_HashSecret { get; set; } = "6JX4T0ZXN430G72BQY54NHUY2D0CUO0R";
        public static string vnp_ReturnUrl { get; set; } = "http://localhost:5187/api/checkin";
    }
}

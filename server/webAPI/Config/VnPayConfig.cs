namespace webAPI.Config
{
    public static class VnPayConfig
    {
        // Sandbox Pay URL
        public const string vnp_PayUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        // Merchant trên VNPay Portal (dùng đúng cặp của bạn)
        public const string vnp_TmnCode = "N5V11ZKB";                               // <-- thay nếu cần
        public const string vnp_HashSecret = "6JX4T0ZXN430G72BQY54NHUY2D0CUO0R";       // <-- thay nếu cần

        // Return URL PHẢI TRÙNG KHỚP với cấu hình trên VNPay portal (dùng ngrok HTTPS)
        public const string vnp_ReturnUrl = "http://localhost:5187/api/buyPackage";
        // ví dụ: "https://bac393e6a1a0.ngrok-free.app/api/buyPackage"
    }
}

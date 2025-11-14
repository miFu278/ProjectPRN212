namespace webAPI.Config
{
    public static class VnPayConfigSwap
    {
        // URL sandbox VNPay
        public const string vnp_PayUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        // Mã Terminal ID dùng cho luồng checkin/swap
        public const string vnp_TmnCode = "N5V11ZKB";

        // Hash secret
        public const string vnp_HashSecret = "6JX4T0ZXN430G72BQY54NHUY2D0CUO0R";

        // URL mà VNPay redirect về sau khi thanh toán xong
        // (nhớ bỏ dấu cách ở đầu như trong code Java của bạn)
        // ví dụ: https://xxxx.ngrok-free.app/webAPI/api/checkin
        public const string vnp_ReturnUrl = "http://localhost:5187/api/checkin";
        // Nếu chạy local:
        // public const string vnp_ReturnUrl = "http://localhost:5000/api/checkin";
    }
}

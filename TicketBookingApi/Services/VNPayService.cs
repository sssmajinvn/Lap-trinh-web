using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TicketBookingApi.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(string orderId, decimal amount, string orderInfo, string ipAddr);
        bool ValidateSignature(IQueryCollection queryData);
    }

    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(string orderId, decimal amount, string orderInfo, string ipAddr)
        {
            var tmnCode = _configuration["VNPay:TmnCode"];
            var hashSecret = _configuration["VNPay:HashSecret"];
            var baseUrl = _configuration["VNPay:Url"];
            var returnUrl = _configuration["VNPay:ReturnUrl"];

            var vnp_Params = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode! },
                { "vnp_Amount", ((long)(amount * 100)).ToString() }, // VNPay requires multiplying by 100
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddr },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl! },
                { "vnp_TxnRef", orderId }
            };

            // Build query string
            var sortedParams = vnp_Params.OrderBy(kv => kv.Key);
            var query = new StringBuilder();
            var hashData = new StringBuilder();

            foreach (var kv in sortedParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // VNPay chuẩn yêu cầu UrlEncode
                    query.Append(VnPayUrlEncode(kv.Key) + "=" + VnPayUrlEncode(kv.Value) + "&");
                }
            }

            var queryString = query.ToString().TrimEnd('&');
            var hashString = queryString; // Trong VNPay v2.1.0, hashString chính là queryString (đã UrlEncode)

            var vnp_SecureHash = HmacSha512(hashSecret!, hashString);
            queryString += "&vnp_SecureHash=" + vnp_SecureHash;

            var finalUrl = baseUrl + "?" + queryString;
            
            // LOG ĐỂ DEBUG VNPay
            Console.WriteLine("=== VNPAY DEBUG INFO ===");
            Console.WriteLine("vnp_Params JSON: " + System.Text.Json.JsonSerializer.Serialize(vnp_Params));
            Console.WriteLine("hashString: " + hashString);
            Console.WriteLine("vnp_SecureHash: " + vnp_SecureHash);
            Console.WriteLine("finalUrl: " + finalUrl);
            Console.WriteLine("========================");

            return finalUrl;
        }

        public bool ValidateSignature(IQueryCollection queryData)
        {
            var hashSecret = _configuration["VNPay:HashSecret"];
            var vnp_SecureHash = queryData["vnp_SecureHash"].ToString();

            var vnp_Params = new Dictionary<string, string>();
            foreach (var kv in queryData)
            {
                if (kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    vnp_Params.Add(kv.Key, kv.Value.ToString());
                }
            }

            var sortedParams = vnp_Params.OrderBy(kv => kv.Key);
            var hashData = new StringBuilder();

            foreach (var kv in sortedParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    hashData.Append(VnPayUrlEncode(kv.Key) + "=" + VnPayUrlEncode(kv.Value) + "&");
                }
            }

            var hashString = hashData.ToString().TrimEnd('&');
            var computedHash = HmacSha512(hashSecret!, hashString);

            return computedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string VnPayUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            // Uri.EscapeDataString dùng chuẩn RFC 3986 (đổi khoảng trắng thành %20, các ký tự khác thành hex in hoa)
            // VNPay backend (PHP) và thư viện vnpay Node.js dùng urlencode/URLSearchParams đổi khoảng trắng thành +
            return Uri.EscapeDataString(value).Replace("%20", "+");
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}

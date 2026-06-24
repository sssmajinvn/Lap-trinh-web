using System.Security.Cryptography;
using System.Text;

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
                { "vnp_Amount", ((long)(amount * 100)).ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddr },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl! },
                { "vnp_TxnRef", orderId }
            };

            var sortedParams = vnp_Params.OrderBy(kv => kv.Key);
            var query = new StringBuilder();

            foreach (var kv in sortedParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(VnPayUrlEncode(kv.Key) + "=" + VnPayUrlEncode(kv.Value) + "&");
                }
            }

            var queryString = query.ToString().TrimEnd('&');
            var hashString = queryString;

            var vnp_SecureHash = HmacSha512(hashSecret!, hashString);
            queryString += "&vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl + "?" + queryString;
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

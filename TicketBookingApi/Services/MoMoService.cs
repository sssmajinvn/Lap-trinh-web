using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TicketBookingApi.Services
{
    public interface IMoMoService
    {
        Task<string?> CreatePaymentUrl(string orderId, decimal amount, string orderInfo);
        bool ValidateSignature(IQueryCollection queryData);
    }

    public class MoMoService : IMoMoService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MoMoService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<string?> CreatePaymentUrl(string orderId, decimal amount, string orderInfo)
        {
            var partnerCode = _configuration["MoMo:PartnerCode"];
            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];
            var apiUrl = _configuration["MoMo:ApiUrl"];
            var redirectUrl = _configuration["MoMo:RedirectUrl"];
            var ipnUrl = _configuration["MoMo:IpnUrl"];

            var amountStr = ((long)amount).ToString();
            var requestId = orderId;
            var requestType = "captureWallet";
            var extraData = "";
            var orderInfoSimple = "ThanhToanVeXemPhim"; // Avoid encoding issues

            var rawSignature = $"accessKey={accessKey}&amount={amountStr}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfoSimple}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            
            var signature = HmacSha256(secretKey!, rawSignature);

            var requestBody = new
            {
                partnerCode,
                requestId,
                amount = amountStr,
                orderId,
                orderInfo = orderInfoSimple,
                redirectUrl,
                ipnUrl,
                requestType,
                extraData,
                lang = "vi",
                signature
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseContent);
                if (json.RootElement.TryGetProperty("resultCode", out var resultCode) && resultCode.GetInt32() == 0)
                {
                    return json.RootElement.GetProperty("payUrl").GetString();
                }
            }

            return null;
        }

        public bool ValidateSignature(IQueryCollection data)
        {
            var partnerCode = data["partnerCode"].ToString();
            var orderId = data["orderId"].ToString();
            var requestId = data["requestId"].ToString();
            var amount = data["amount"].ToString();
            var orderInfo = data["orderInfo"].ToString();
            var orderType = data["orderType"].ToString();
            var transId = data["transId"].ToString();
            var resultCode = data["resultCode"].ToString();
            var message = data["message"].ToString();
            var payType = data["payType"].ToString();
            var responseTime = data["responseTime"].ToString();
            var extraData = data["extraData"].ToString();
            var receivedSignature = data["signature"].ToString();

            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];

            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

            var computedSignature = HmacSha256(secretKey!, rawSignature);

            return computedSignature.Equals(receivedSignature, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string HmacSha256(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                var hash = new StringBuilder();
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
                return hash.ToString();
            }
        }
    }
}

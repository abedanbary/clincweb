using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClinicApp.Web.Services
{
    public interface IWhatsAppService
    {
        Task<(bool Success, string Message)> SendTemplateMessageAsync(string toPhone, string templateName = "hello_world");
        Task<(bool Success, string Message)> SendTextMessageAsync(string toPhone, string text);
        bool IsConfigured { get; }
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string? _accessToken;
        private readonly string? _phoneNumberId;

        public bool IsConfigured { get; }

        public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _accessToken = configuration["WHATSAPP_ACCESS_TOKEN"];
            _phoneNumberId = configuration["WHATSAPP_PHONE_NUMBER_ID"];
            IsConfigured = !string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_phoneNumberId);

            if (IsConfigured)
                _logger.LogInformation("[WhatsApp] Cloud API configured. PhoneNumberId ends in ...{Suffix}",
                    _phoneNumberId![^4..]);
            else
                _logger.LogWarning("[WhatsApp] NOT configured — WHATSAPP_ACCESS_TOKEN or WHATSAPP_PHONE_NUMBER_ID is missing.");
        }

        public async Task<(bool Success, string Message)> SendTemplateMessageAsync(string toPhone, string templateName = "hello_world")
        {
            if (!IsConfigured)
                return (false, "WhatsApp not configured: WHATSAPP_ACCESS_TOKEN or WHATSAPP_PHONE_NUMBER_ID missing.");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "en_US" }
                }
            };

            return await SendRequestAsync(payload, toPhone);
        }

        public async Task<(bool Success, string Message)> SendTextMessageAsync(string toPhone, string text)
        {
            if (!IsConfigured)
                return (false, "WhatsApp not configured: WHATSAPP_ACCESS_TOKEN or WHATSAPP_PHONE_NUMBER_ID missing.");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone,
                type = "text",
                text = new { body = text }
            };

            return await SendRequestAsync(payload, toPhone);
        }

        private async Task<(bool Success, string Message)> SendRequestAsync(object payload, string toPhone)
        {
            var masked = toPhone.Length > 7 ? toPhone[..4] + "****" + toPhone[^3..] : "****";
            try
            {
                var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);

                _logger.LogInformation("[WhatsApp] POST to Meta API for {MaskedPhone}...", masked);
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WhatsApp] Sent successfully to {MaskedPhone}. Status: {Status}",
                        masked, (int)response.StatusCode);
                    return (true, "Message sent successfully.");
                }
                else
                {
                    _logger.LogError("[WhatsApp] Meta API rejected request for {MaskedPhone}. Status: {Status} | Body: {Body}",
                        masked, (int)response.StatusCode, responseBody);
                    return (false, $"API error {(int)response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhatsApp] Exception while sending to {MaskedPhone}.", masked);
                return (false, $"Exception: {ex.Message}");
            }
        }
    }
}

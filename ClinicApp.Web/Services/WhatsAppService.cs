using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClinicApp.Web.Services
{
    public interface IWhatsAppService
    {
        Task<(bool Success, string Message)> SendTemplateMessageAsync(string toPhone, string templateName = "hello_world");
        Task<(bool Success, string Message)> SendTextMessageAsync(string toPhone, string text);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _accessToken;
        private readonly string? _phoneNumberId;
        private readonly bool _isConfigured;

        public WhatsAppService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _accessToken = configuration["WHATSAPP_ACCESS_TOKEN"];
            _phoneNumberId = configuration["WHATSAPP_PHONE_NUMBER_ID"];
            _isConfigured = !string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_phoneNumberId);

            if (_isConfigured)
                Console.WriteLine("✅ WhatsApp Cloud API configured successfully");
            else
                Console.WriteLine("⚠️ WhatsApp Cloud API not configured (WHATSAPP_ACCESS_TOKEN or WHATSAPP_PHONE_NUMBER_ID missing)");
        }

        public async Task<(bool Success, string Message)> SendTemplateMessageAsync(string toPhone, string templateName = "hello_world")
        {
            if (!_isConfigured)
                return (false, "WhatsApp is not configured. Set WHATSAPP_ACCESS_TOKEN and WHATSAPP_PHONE_NUMBER_ID environment variables.");

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

            return await SendRequestAsync(payload);
        }

        public async Task<(bool Success, string Message)> SendTextMessageAsync(string toPhone, string text)
        {
            if (!_isConfigured)
                return (false, "WhatsApp is not configured. Set WHATSAPP_ACCESS_TOKEN and WHATSAPP_PHONE_NUMBER_ID environment variables.");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone,
                type = "text",
                text = new { body = text }
            };

            return await SendRequestAsync(payload);
        }

        private async Task<(bool Success, string Message)> SendRequestAsync(object payload)
        {
            try
            {
                var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);

                Console.WriteLine($"📤 Sending WhatsApp message to Meta API...");
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ WhatsApp message sent successfully");
                    return (true, "Message sent successfully.");
                }
                else
                {
                    Console.WriteLine($"❌ WhatsApp API error: {response.StatusCode} - {responseBody}");
                    return (false, $"API error {(int)response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WhatsApp exception: {ex.Message}");
                return (false, $"Error sending message: {ex.Message}");
            }
        }
    }
}

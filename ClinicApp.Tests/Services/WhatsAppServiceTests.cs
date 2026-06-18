using ClinicApp.Web.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace ClinicApp.Tests.Services;

public class WhatsAppServiceTests
{
    private static IConfiguration BuildConfig(string? token = "test-token", string? phoneId = "123456")
    {
        var data = new Dictionary<string, string?>();
        if (token != null) data["WHATSAPP_ACCESS_TOKEN"] = token;
        if (phoneId != null) data["WHATSAPP_PHONE_NUMBER_ID"] = phoneId;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static HttpClient BuildHttpClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseBody)
            });
        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task SendTemplateMessage_NotConfigured_ReturnsFailure()
    {
        var config = BuildConfig(token: null, phoneId: null);
        var service = new WhatsAppService(new HttpClient(), config);

        var (success, message) = await service.SendTemplateMessageAsync("972501234567");

        Assert.False(success);
        Assert.Contains("not configured", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendTemplateMessage_ApiSuccess_ReturnsSuccess()
    {
        var config = BuildConfig();
        var httpClient = BuildHttpClient(HttpStatusCode.OK, "{\"messages\":[{\"id\":\"wamid.test\"}]}");
        var service = new WhatsAppService(httpClient, config);

        var (success, message) = await service.SendTemplateMessageAsync("972501234567");

        Assert.True(success);
        Assert.Contains("success", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendTemplateMessage_ApiError_ReturnsFailure()
    {
        var config = BuildConfig();
        var httpClient = BuildHttpClient(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"Invalid token\"}}");
        var service = new WhatsAppService(httpClient, config);

        var (success, message) = await service.SendTemplateMessageAsync("972501234567");

        Assert.False(success);
        Assert.Contains("401", message);
    }

    [Fact]
    public async Task SendTextMessage_NotConfigured_ReturnsFailure()
    {
        var config = BuildConfig(token: null);
        var service = new WhatsAppService(new HttpClient(), config);

        var (success, message) = await service.SendTextMessageAsync("972501234567", "Hello");

        Assert.False(success);
        Assert.Contains("not configured", message, StringComparison.OrdinalIgnoreCase);
    }
}

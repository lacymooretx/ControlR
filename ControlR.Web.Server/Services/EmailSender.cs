using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ControlR.Web.Server.Services;

public class EmailSender(
  IHttpClientFactory httpClientFactory,
  IHttpContextAccessor httpContextAccessor,
  IOptionsMonitor<AppOptions> appOptions,
  ILogger<EmailSender> logger) : IEmailSender
{
  private readonly IOptionsMonitor<AppOptions> _appOptions = appOptions;
  private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
  private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
  private readonly ILogger<EmailSender> _logger = logger;

  public async Task SendEmailAsync(string email, string subject, string htmlMessage)
  {
    try
    {
      var currentOptions = _appOptions.CurrentValue;

      if (currentOptions.DisableEmailSending)
      {
        _logger.LogInformation(
          "Email sending is disabled.  Email to \"{ToEmail}\" with subject \"{Subject}\" will not be sent.",
          email,
          subject);

        return;
      }

      if (string.IsNullOrWhiteSpace(currentOptions.Smtp2GoApiKey))
      {
        _logger.LogCritical("SMTP2GO API key is not configured. Unable to send email.");
        return;
      }

      if (string.IsNullOrWhiteSpace(currentOptions.SmtpEmail))
      {
        _logger.LogCritical("Sender email (SmtpEmail) is not configured. Unable to send email.");
        return;
      }

      var body = PrepareHtmlBody(htmlMessage);

      var request = new Smtp2GoSendRequest
      {
        Sender = FormatSender(currentOptions),
        To = [email],
        Subject = subject,
        HtmlBody = body
      };

      using var client = _httpClientFactory.CreateClient("Smtp2Go");
      client.DefaultRequestHeaders.Add("X-Smtp2go-Api-Key", currentOptions.Smtp2GoApiKey);

      var response = await client.PostAsJsonAsync(
        "https://api.smtp2go.com/v3/email/send",
        request,
        Smtp2GoJsonContext.Default.Smtp2GoSendRequest);

      if (!response.IsSuccessStatusCode)
      {
        var errorBody = await response.Content.ReadAsStringAsync();
        _logger.LogError(
          "SMTP2GO API returned {StatusCode}: {ErrorBody}",
          response.StatusCode,
          errorBody);
        throw new InvalidOperationException($"SMTP2GO API error: {response.StatusCode}");
      }

      var result = await response.Content.ReadFromJsonAsync(
        Smtp2GoJsonContext.Default.Smtp2GoSendResponse);

      if (result?.Data?.Failed > 0)
      {
        _logger.LogError(
          "SMTP2GO reported {FailedCount} failure(s) for email to {ToEmail}: {Failures}",
          result.Data.Failed,
          email,
          string.Join(", ", result.Data.Failures ?? []));
        throw new InvalidOperationException($"SMTP2GO send failed for {email}");
      }

      _logger.LogInformation(
        "Email successfully sent to {ToEmail} via SMTP2GO. Subject: \"{Subject}\", EmailId: {EmailId}",
        email,
        subject,
        result?.Data?.EmailId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending email.");
      throw;
    }
  }

  private static string FormatSender(AppOptions options)
  {
    if (!string.IsNullOrWhiteSpace(options.SmtpDisplayName))
    {
      return $"{options.SmtpDisplayName} <{options.SmtpEmail}>";
    }
    return options.SmtpEmail!;
  }

  private string PrepareHtmlBody(string htmlMessage)
  {
    if (TryGetLogoHtml(out var logoHtml))
    {
      return $"{logoHtml}<br/>{htmlMessage}";
    }
    return htmlMessage;
  }

  private bool TryGetLogoHtml([NotNullWhen(true)] out string? logoHtml)
  {
    if (_httpContextAccessor.HttpContext?.Request is not { } request)
    {
      logoHtml = null;
      return false;
    }

    if (request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
      logoHtml = null;
      return false;
    }

    var imageUrl = new Uri(request.ToOrigin(), "/images/company-logo.png");

    logoHtml = $"""
      <img
        src="{imageUrl}"
        alt="Company Logo"
        width="256" />
    """;
    return true;
  }
}

public class Smtp2GoSendRequest
{
  [JsonPropertyName("sender")]
  public required string Sender { get; init; }

  [JsonPropertyName("to")]
  public required string[] To { get; init; }

  [JsonPropertyName("subject")]
  public required string Subject { get; init; }

  [JsonPropertyName("html_body")]
  public string? HtmlBody { get; init; }
}

public class Smtp2GoSendResponse
{
  [JsonPropertyName("request_id")]
  public string? RequestId { get; init; }

  [JsonPropertyName("data")]
  public Smtp2GoSendResponseData? Data { get; init; }
}

public class Smtp2GoSendResponseData
{
  [JsonPropertyName("succeeded")]
  public int Succeeded { get; init; }

  [JsonPropertyName("failed")]
  public int Failed { get; init; }

  [JsonPropertyName("failures")]
  public string[]? Failures { get; init; }

  [JsonPropertyName("email_id")]
  public string? EmailId { get; init; }
}

[JsonSerializable(typeof(Smtp2GoSendRequest))]
[JsonSerializable(typeof(Smtp2GoSendResponse))]
internal partial class Smtp2GoJsonContext : JsonSerializerContext;

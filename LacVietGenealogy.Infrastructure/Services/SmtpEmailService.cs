using System.Net;
using System.Net.Mail;
using LacVietGenealogy.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LacVietGenealogy.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var from = _configuration["Smtp:From"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP configuration is incomplete. Email not sent to {ToEmail}. Host={Host}, Port={Port}, From={From}, HasUsername={HasUsername}, HasPassword={HasPassword}", toEmail, host, port, from, !string.IsNullOrEmpty(username), !string.IsNullOrEmpty(password));
                return false;
            }

            _logger.LogInformation("Sending email to {ToEmail} via {Host}:{Port} from {From} with subject {Subject}", toEmail, host, port, from ?? username, subject);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(from ?? username),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            if (ex is SmtpException smtpException && smtpException.Message.Contains("5.4.5", StringComparison.OrdinalIgnoreCase))
                _logger.LogError("SMTP provider rejected the message because the daily sending limit was exceeded for {ToEmail}. Configure another SMTP account/provider or wait for the limit to reset.", toEmail);
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }
}

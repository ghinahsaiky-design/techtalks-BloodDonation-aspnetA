using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BloodDonation.Data;
using BloodDonation.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BloodDonation.Services
{
    public class EmailMonitoringService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailMonitoringService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public EmailMonitoringService(
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EmailMonitoringService> logger)
        {
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Monitoring Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForEmailRepliesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in email monitoring service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckForEmailRepliesAsync()
        {
            var emailEnabled = _configuration.GetValue<bool>("EmailMonitoring:Enabled", false);
            if (!emailEnabled)
            {
                return; // Email monitoring is disabled
            }

            var imapHost = _configuration["EmailMonitoring:ImapHost"] ?? "imap.gmail.com";
            var imapPort = _configuration.GetValue<int>("EmailMonitoring:ImapPort", 993);
            var emailUsername = _configuration["EmailMonitoring:EmailUsername"] ?? _configuration["EmailSettings:SmtpUsername"];
            var emailPassword = _configuration["EmailMonitoring:EmailPassword"] ?? _configuration["EmailSettings:SmtpPassword"];

            if (string.IsNullOrEmpty(emailUsername) || string.IsNullOrEmpty(emailPassword))
            {
                _logger.LogWarning("Email monitoring credentials not configured. Skipping email check.");
                return;
            }

            try
            {
                using (var client = new ImapClient())
                {
                    await client.ConnectAsync(imapHost, imapPort, true);
                    await client.AuthenticateAsync(emailUsername, emailPassword);

                    // Open the Inbox folder in ReadWrite mode to mark emails as read
                    var inbox = client.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadWrite);

                    // Search for unread emails from the last 24 hours
                    var searchQuery = SearchQuery.And(
                        SearchQuery.NotSeen,
                        SearchQuery.DeliveredAfter(DateTime.Now.AddDays(-1))
                    );

                    var uids = await inbox.SearchAsync(searchQuery);

                    _logger.LogInformation($"Found {uids.Count} unread emails to process");

                    var processedCount = 0;
                    foreach (var uid in uids)
                    {
                        try
                        {
                            var message = await inbox.GetMessageAsync(uid);
                            var processed = await ProcessEmailReplyAsync(message, uid, inbox);
                            if (processed)
                            {
                                processedCount++;
                                // Mark email as read after processing
                                await inbox.SetFlagsAsync(uid, MessageFlags.Seen, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing email UID {uid}");
                        }
                    }

                    if (processedCount > 0)
                    {
                        _logger.LogInformation($"Processed {processedCount} confirmation email(s)");
                    }

                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to IMAP server");
            }
        }

        private async Task<bool> ProcessEmailReplyAsync(MimeMessage message, UniqueId uid, IMailFolder inbox)
        {
            try
            {
                // Extract Request ID and Donor ID from email headers
                var requestIdHeader = message.Headers["X-Request-ID"];
                var donorIdHeader = message.Headers["X-Donor-ID"];

                // Also try to extract from subject (fallback)
                var subjectRequestId = ExtractRequestIdFromSubject(message.Subject);

                int? requestId = null;
                int? donorId = null;

                if (!string.IsNullOrEmpty(requestIdHeader) && int.TryParse(requestIdHeader, out var reqId))
                {
                    requestId = reqId;
                }
                else if (subjectRequestId.HasValue)
                {
                    requestId = subjectRequestId;
                }

                if (!string.IsNullOrEmpty(donorIdHeader) && int.TryParse(donorIdHeader, out var donId))
                {
                    donorId = donId;
                }

                if (!requestId.HasValue)
                {
                    _logger.LogWarning($"Email from {message.From} has no Request ID. Skipping.");
                    return false;
                }

                // Extract email body text
                var bodyText = GetEmailBodyText(message);

                // Check if this is a confirmation reply
                var isConfirmation = IsConfirmationReply(bodyText);

                if (!isConfirmation)
                {
                    _logger.LogInformation($"Email from {message.From} for Request {requestId} is not a confirmation. Skipping.");
                    return false;
                }

                // Determine donor ID if not in headers
                if (!donorId.HasValue)
                {
                    donorId = await FindDonorIdByEmailAsync(message.From.Mailboxes.FirstOrDefault()?.Address, requestId.Value);
                }

                if (!donorId.HasValue)
                {
                    _logger.LogWarning($"Could not determine Donor ID for email from {message.From} for Request {requestId}. Skipping.");
                    return false;
                }

                // Create or update confirmation
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<BloodDonationContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    var existingConfirmation = await context.DonorConfirmations
                        .FirstOrDefaultAsync(c => c.RequestId == requestId.Value && c.DonorId == donorId.Value);

                    var isNewConfirmation = false;
                    var shouldSendEmail = false;

                    if (existingConfirmation != null)
                    {
                        // Update existing confirmation
                        var wasNotConfirmed = existingConfirmation.Status != "Confirmed";
                        existingConfirmation.Status = "Confirmed";
                        existingConfirmation.Message = bodyText.Length > 500 ? bodyText.Substring(0, 500) : bodyText;
                        existingConfirmation.ConfirmedAt = DateTime.UtcNow;
                        shouldSendEmail = wasNotConfirmed;
                        _logger.LogInformation($"Updated confirmation for Request {requestId}, Donor {donorId}");
                    }
                    else
                    {
                        // Create new confirmation
                        var confirmation = new DonorConfirmation
                        {
                            RequestId = requestId.Value,
                            DonorId = donorId.Value,
                            Status = "Confirmed",
                            Message = bodyText.Length > 500 ? bodyText.Substring(0, 500) : bodyText,
                            ConfirmedAt = DateTime.UtcNow
                        };

                        context.DonorConfirmations.Add(confirmation);
                        isNewConfirmation = true;
                        shouldSendEmail = true;
                        _logger.LogInformation($"Created new confirmation for Request {requestId}, Donor {donorId}");
                    }

                    await context.SaveChangesAsync();

                    // Send email to requester if this is a new confirmation
                    if (shouldSendEmail)
                    {
                        var request = await context.DonorRequests
                            .Include(r => r.BloodType)
                            .Include(r => r.Location)
                            .FirstOrDefaultAsync(r => r.RequestId == requestId.Value);

                        if (request != null && !string.IsNullOrEmpty(request.RequesterEmail))
                        {
                            var donor = await context.DonorProfile
                                .Include(d => d.User)
                                .Include(d => d.BloodType)
                                .Include(d => d.Location)
                                .FirstOrDefaultAsync(d => d.DonorId == donorId.Value);

                            if (donor != null)
                            {
                                try
                                {
                                    await notificationService.SendDonorInfoToRequesterAsync(donor, request);
                                    _logger.LogInformation($"Sent donor info to requester {request.RequesterEmail} for request {requestId}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error sending donor info to requester for request {requestId}");
                                }
                            }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing email reply from {message.From}");
            }

            return false;
        }

        private bool IsConfirmationReply(string bodyText)
        {
            if (string.IsNullOrWhiteSpace(bodyText))
                return false;

            var normalizedText = bodyText.ToLowerInvariant().Trim();

            // Patterns that indicate confirmation
            var confirmationPatterns = new[]
            {
                @"\b(yes|yeah|yep|yup|sure|ok|okay|confirmed|confirm|i can|i will|available|ready|willing|accept|agree)\b",
                @"\b(i can help|i can donate|i'm available|i am available|count me in|i'm in|sign me up)\b",
                @"\b(ready to donate|willing to help|happy to help|glad to help)\b"
            };

            foreach (var pattern in confirmationPatterns)
            {
                if (Regex.IsMatch(normalizedText, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            // Check for negative responses
            var negativePatterns = new[]
            {
                @"\b(no|nope|sorry|unavailable|can't|cannot|decline|refuse|not available)\b"
            };

            foreach (var pattern in negativePatterns)
            {
                if (Regex.IsMatch(normalizedText, pattern, RegexOptions.IgnoreCase))
                {
                    return false; // Explicitly not a confirmation
                }
            }

            return false;
        }

        private string GetEmailBodyText(MimeMessage message)
        {
            if (message.TextBody != null)
            {
                return message.TextBody;
            }

            if (message.HtmlBody != null)
            {
                // Simple HTML tag removal (for basic cases)
                return Regex.Replace(message.HtmlBody, "<[^>]*>", " ");
            }

            return string.Empty;
        }

        private int? ExtractRequestIdFromSubject(string subject)
        {
            if (string.IsNullOrEmpty(subject))
                return null;

            // Look for patterns like "Request #123" or "RE: Request #123" or "[Request #123]"
            var match = Regex.Match(subject, @"(?:request|req)[\s#:]*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var requestId))
            {
                return requestId;
            }

            return null;
        }

        private async Task<int?> FindDonorIdByEmailAsync(string? emailAddress, int requestId)
        {
            if (string.IsNullOrEmpty(emailAddress))
                return null;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BloodDonationContext>();

                // Find donor by email who matches the request
                var request = await context.DonorRequests
                    .Include(r => r.BloodType)
                    .Include(r => r.Location)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                    return null;

                var donor = await context.DonorProfile
                    .Include(d => d.User)
                    .Where(d => d.User.Email == emailAddress &&
                               d.BloodTypeId == request.BloodTypeId &&
                               d.LocationId == request.LocationId)
                    .FirstOrDefaultAsync();

                return donor?.DonorId;
            }
        }
    }
}


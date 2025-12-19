using System.Net;
using System.Net.Mail;
using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BloodDonation.Services
{
    public class NotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly BloodDonationContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IConfiguration configuration,
            BloodDonationContext context,
            ILogger<NotificationService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Sends email notification to a donor about a new blood request
        /// </summary>
        public async Task<bool> SendEmailNotificationAsync(Users donor, DonorRequest request)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "BloodConnect";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email notification skipped.");
                    return false;
                }

                var donorName = $"{donor.FirstName} {donor.LastName}";
                var bloodType = request.BloodType?.Type ?? "Unknown";
                var location = request.Location?.Districts ?? "Unknown";
                var urgency = request.UrgencyLevel;
                var hospitalName = !string.IsNullOrEmpty(request.HospitalName) ? request.HospitalName : "Not specified";
                var contactNumber = request.ContactNumber;
                var additionalNotes = !string.IsNullOrEmpty(request.AdditionalNotes) ? request.AdditionalNotes : "None";

                var subject = $"🚨 Urgent Blood Donation Request - {bloodType} Needed";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #d70427; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .urgent {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .info-box {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; border: 1px solid #e0e0e0; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #d70427; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Blood Donation Request</h1>
        </div>
        <div class=""content"">
            <p>Dear {donorName},</p>
            <p>A new blood donation request has been created that matches your blood type and location.</p>
            
            <div class=""urgent"">
                <strong>Urgency Level: {urgency}</strong>
            </div>
            
            <div class=""info-box"">
                <h3>Request Details:</h3>
                <p><strong>Blood Type Needed:</strong> {bloodType}</p>
                <p><strong>Location:</strong> {location}</p>
                <p><strong>Patient Name:</strong> {request.PatientName}</p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>Contact Number:</strong> {contactNumber}</p>
                <p><strong>Additional Notes:</strong> {additionalNotes}</p>
            </div>
            
            <p>If you are available and eligible to donate, please contact the number above as soon as possible.</p>
            
            <p>Thank you for being a lifesaver!</p>
            
            <div class=""footer"">
                <p>This is an automated message from BloodConnect.</p>
                <p>If you no longer wish to receive these notifications, please update your preferences in your account settings.</p>
            </div>
        </div>
    </div>
</body>
</html>";

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(donor.Email);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email notification sent to {donor.Email} for request {request.RequestId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email notification to {donor.Email} for request {request.RequestId}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS notification to a donor about a new blood request
        /// Note: This is a placeholder implementation. Configure with Twilio or another SMS service.
        /// </summary>
        public async Task<bool> SendSmsNotificationAsync(Users donor, DonorRequest request)
        {
            try
            {
                var phoneNumber = donor.PhoneNumber;
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    _logger.LogWarning($"No phone number found for donor {donor.Id}. SMS skipped.");
                    return false;
                }

                // TODO: Integrate with SMS service (Twilio, AWS SNS, etc.)
                // For now, we'll log the SMS that would be sent
                var bloodType = request.BloodType?.Type ?? "Unknown";
                var location = request.Location?.Districts ?? "Unknown";
                var urgency = request.UrgencyLevel;
                var message = $"BloodConnect: Urgent {bloodType} blood needed in {location}. Urgency: {urgency}. Contact: {request.ContactNumber}. Request ID: {request.RequestId}";

                _logger.LogInformation($"SMS would be sent to {phoneNumber}: {message}");

                // Placeholder for actual SMS sending
                // Example with Twilio:
                // var twilioClient = new TwilioRestClient(accountSid, authToken);
                // var message = await twilioClient.Messages.CreateAsync(
                //     new PhoneNumber(phoneNumber),
                //     from: new PhoneNumber(twilioPhoneNumber),
                //     body: message
                // );

                // For development, you can use a service like Twilio
                // Add Twilio package: dotnet add package Twilio
                // Configure in appsettings.json:
                // "SmsSettings": {
                //     "TwilioAccountSid": "your_account_sid",
                //     "TwilioAuthToken": "your_auth_token",
                //     "TwilioPhoneNumber": "your_twilio_number"
                // }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS notification to {donor.PhoneNumber} for request {request.RequestId}");
                return false;
            }
        }

        /// <summary>
        /// Finds all matching donors for a blood request and sends notifications
        /// </summary>
        public async Task<int> NotifyMatchingDonorsAsync(DonorRequest request)
        {
            try
            {
                // Load related data
                await _context.Entry(request)
                    .Reference(r => r.BloodType)
                    .LoadAsync();
                await _context.Entry(request)
                    .Reference(r => r.Location)
                    .LoadAsync();

                // Find matching donors:
                // 1. Same blood type
                // 2. Same location (or nearby if needed)
                // 3. Available and healthy for donation
                var matchingDonors = await _context.DonorProfile
                    .Include(d => d.User)
                    .Include(d => d.BloodType)
                    .Include(d => d.Location)
                    .Where(d => 
                        d.BloodTypeId == request.BloodTypeId &&
                        d.LocationId == request.LocationId &&
                        d.IsAvailable &&
                        d.IsHealthyForDonation)
                    .ToListAsync();

                if (!matchingDonors.Any())
                {
                    _logger.LogInformation($"No matching donors found for request {request.RequestId}");
                    return 0;
                }

                _logger.LogInformation($"Found {matchingDonors.Count} matching donors for request {request.RequestId}");

                int emailCount = 0;
                int smsCount = 0;

                // Send notifications to all matching donors
                foreach (var donor in matchingDonors)
                {
                    if (donor.User != null)
                    {
                        // Send email notification
                        var emailSent = await SendEmailNotificationAsync(donor.User, request);
                        if (emailSent) emailCount++;

                        // Send SMS notification
                        var smsSent = await SendSmsNotificationAsync(donor.User, request);
                        if (smsSent) smsCount++;
                    }
                }

                _logger.LogInformation($"Notifications sent for request {request.RequestId}: {emailCount} emails, {smsCount} SMS");
                return emailCount + smsCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify matching donors for request {request.RequestId}");
                return 0;
            }
        }
    }
}


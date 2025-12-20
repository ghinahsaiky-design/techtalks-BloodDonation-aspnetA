using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
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
        /// Sends email notification to a donor about a new blood request with reply-to confirmation
        /// </summary>
        public async Task<bool> SendEmailNotificationToDonorAsync(Users donor, DonorRequest request)
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

                // Extract domain from email address for reply-to tracking
                var emailDomain = fromEmail.Contains("@") ? fromEmail.Split('@')[1] : "bloodconnect.com";
                var replyToEmail = $"request-{request.RequestId}@{emailDomain}";
                // For Gmail, use a simpler approach - reply-to the from email with a subject tag
                var replyToAddress = fromEmail;

                var subject = $"🚨 Urgent Blood Donation Request - {bloodType} Needed [Request #{request.RequestId}]";
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
        .confirmation-box {{ background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin: 15px 0; }}
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
                <p><strong>Request ID:</strong> #{request.RequestId}</p>
                <p><strong>Blood Type Needed:</strong> {bloodType}</p>
                <p><strong>Location:</strong> {location}</p>
                <p><strong>Patient Name:</strong> {request.PatientName}</p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>Contact Number:</strong> {contactNumber}</p>
                <p><strong>Additional Notes:</strong> {additionalNotes}</p>
            </div>
            
            <div class=""confirmation-box"">
                <h3>📧 Reply to Confirm</h3>
                <p>If you are available and willing to donate, please reply to this email with:</p>
                <ul>
                    <li><strong>""YES""</strong> or <strong>""I can help""</strong> - to confirm your availability</li>
                    <li>Your preferred contact time (if any)</li>
                </ul>
                <p>Your reply will be automatically tracked and the admin will be notified of your confirmation.</p>
            </div>
            
            <p>If you are available and eligible to donate, please contact the number above as soon as possible.</p>
            
            <p>Thank you for being a lifesaver!</p>
            
            <div class=""footer"">
                <p>This is an automated message from BloodConnect.</p>
                <p><strong>Request ID: #{request.RequestId}</strong> - Please include this in your reply.</p>
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
                        IsBodyHtml = true,
                        ReplyTo = new MailAddress(replyToAddress, $"BloodConnect Request #{request.RequestId}")
                    };

                    mailMessage.To.Add(donor.Email);
                    mailMessage.Headers.Add("X-Request-ID", request.RequestId.ToString());
                    mailMessage.Headers.Add("X-Donor-ID", donor.Id.ToString());

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
        /// Sends donor information to the requester when a donor confirms availability
        /// </summary>
        public async Task<bool> SendDonorInfoToRequesterAsync(DonorProfile donor, DonorRequest request)
        {
            try
            {
                _logger.LogInformation($"SendDonorInfoToRequesterAsync called for Request {request.RequestId}, RequesterEmail: '{request.RequesterEmail}'");

                if (string.IsNullOrWhiteSpace(request.RequesterEmail))
                {
                    _logger.LogWarning($"No requester email found for request {request.RequestId}. Cannot send donor information.");
                    return false;
                }

                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "BloodConnect";

                _logger.LogInformation($"SMTP Configuration - Host: {smtpHost}, Port: {smtpPort}, FromEmail: {fromEmail}, Username: {(string.IsNullOrEmpty(smtpUsername) ? "NOT SET" : "SET")}");

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email notification skipped.");
                    return false;
                }

                // Load related data
                await _context.Entry(donor)
                    .Reference(d => d.User)
                    .LoadAsync();
                await _context.Entry(donor)
                    .Reference(d => d.BloodType)
                    .LoadAsync();
                await _context.Entry(donor)
                    .Reference(d => d.Location)
                    .LoadAsync();
                await _context.Entry(request)
                    .Reference(r => r.BloodType)
                    .LoadAsync();
                await _context.Entry(request)
                    .Reference(r => r.Location)
                    .LoadAsync();

                var donorName = donor.IsIdentityHidden ? $"Donor #{donor.DonorId}" : $"{donor.User.FirstName} {donor.User.LastName}";
                var donorEmail = donor.User.Email;
                var donorPhone = donor.User.PhoneNumber ?? "Not provided";
                var donorBloodType = donor.BloodType?.Type ?? "Unknown";
                var donorLocation = donor.Location?.Districts ?? "Unknown";
                var lastDonation = donor.LastDonationDate.HasValue 
                    ? donor.LastDonationDate.Value.ToString("yyyy-MM-dd") 
                    : "Never";

                var subject = $"✅ Donor Confirmed - Request #{request.RequestId} - {request.PatientName}";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4caf50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .success-box {{ background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin: 15px 0; }}
        .info-box {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; border: 1px solid #e0e0e0; }}
        .donor-info {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4caf50; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .contact-info {{ background-color: #e3f2fd; padding: 15px; margin: 15px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Donor Confirmed!</h1>
        </div>
        <div class=""content"">
            <div class=""success-box"">
                <h2>Great News!</h2>
                <p>A donor has confirmed their availability for your blood donation request.</p>
            </div>
            
            <div class=""info-box"">
                <h3>Request Details:</h3>
                <p><strong>Request ID:</strong> #{request.RequestId}</p>
                <p><strong>Patient Name:</strong> {request.PatientName}</p>
                <p><strong>Blood Type Needed:</strong> {request.BloodType?.Type ?? "Unknown"}</p>
                <p><strong>Location:</strong> {request.Location?.Districts ?? "Unknown"}</p>
                <p><strong>Urgency Level:</strong> {request.UrgencyLevel}</p>
                <p><strong>Hospital:</strong> {(!string.IsNullOrEmpty(request.HospitalName) ? request.HospitalName : "Not specified")}</p>
            </div>
            
            <div class=""donor-info"">
                <h3>📞 Donor Contact Information:</h3>
                <p><strong>Name:</strong> {donorName}</p>
                <p><strong>Email:</strong> <a href=""mailto:{donorEmail}"">{donorEmail}</a></p>
                <p><strong>Phone Number:</strong> {donorPhone}</p>
                <p><strong>Blood Type:</strong> {donorBloodType}</p>
                <p><strong>Location:</strong> {donorLocation}</p>
                <p><strong>Last Donation:</strong> {lastDonation}</p>
                <p><strong>Available for Donation:</strong> {(donor.IsAvailable && donor.IsHealthyForDonation ? "Yes ✓" : "Please verify")}</p>
            </div>
            
            <div class=""contact-info"">
                <h3>📧 Next Steps:</h3>
                <ol>
                    <li>Contact the donor using the information provided above</li>
                    <li>Coordinate the donation time and location</li>
                    <li>Confirm the appointment details</li>
                    <li>Thank the donor for their willingness to help!</li>
                </ol>
            </div>
            
            <p><strong>Important:</strong> Please contact the donor as soon as possible to coordinate the donation. Time is critical for blood donation requests.</p>
            
            <div class=""footer"">
                <p>This is an automated message from BloodConnect.</p>
                <p>Request ID: #{request.RequestId}</p>
                <p>If you have any questions, please contact the BloodConnect support team.</p>
            </div>
        </div>
    </div>
</body>
</html>";

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.Timeout = 30000; // 30 second timeout

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(request.RequesterEmail);

                    _logger.LogInformation($"Sending email to requester {request.RequesterEmail} for request {request.RequestId} using SMTP {smtpHost}:{smtpPort}");
                    
                    await client.SendMailAsync(mailMessage);
                    
                    _logger.LogInformation($"Successfully sent donor information email to requester {request.RequesterEmail} for request {request.RequestId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send donor information to requester {request.RequesterEmail} for request {request.RequestId}");
                return false;
            }
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
        /// Supports multiple providers: EmailToSms (free), AWSSNS (free tier), Twilio, Vonage, TextLocal
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

                var smsEnabled = _configuration.GetValue<bool>("SmsSettings:Enabled", false);
                if (!smsEnabled)
                {
                    _logger.LogInformation("SMS notifications are disabled in configuration.");
                    return false;
                }

                var provider = _configuration["SmsSettings:Provider"] ?? "EmailToSms";
                var bloodType = request.BloodType?.Type ?? "Unknown";
                var location = request.Location?.Districts ?? "Unknown";
                var urgency = request.UrgencyLevel;
                var message = $"BloodConnect: Urgent {bloodType} blood needed in {location}. Urgency: {urgency}. Contact: {request.ContactNumber}. Request ID: {request.RequestId}";

                return provider.ToLower() switch
                {
                    "emailtosms" => await SendSmsViaEmailAsync(phoneNumber, message),
                    "awssns" => await SendSmsViaAwsSnsAsync(phoneNumber, message),
                    "twilio" => await SendSmsViaTwilioAsync(phoneNumber, message),
                    "vonage" => await SendSmsViaVonageAsync(phoneNumber, message),
                    "textlocal" => await SendSmsViaTextLocalAsync(phoneNumber, message),
                    _ => await SendSmsViaEmailAsync(phoneNumber, message) // Default to free Email-to-SMS
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS notification to {donor.PhoneNumber} for request {request.RequestId}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS using Email-to-SMS gateway (100% FREE, but less reliable)
        /// Works by sending email to carrier-specific email addresses that forward to SMS
        /// </summary>
        private async Task<bool> SendSmsViaEmailAsync(string phoneNumber, string message)
        {
            try
            {
                // Remove any non-digit characters from phone number
                var cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
                
                // If number starts with country code, extract last 10 digits (US format)
                if (cleanNumber.Length > 10)
                {
                    cleanNumber = cleanNumber.Substring(cleanNumber.Length - 10);
                }

                if (cleanNumber.Length != 10)
                {
                    _logger.LogWarning($"Invalid phone number format for Email-to-SMS: {phoneNumber}");
                    return false;
                }

                // Common US carrier email-to-SMS gateways
                var carriers = new[]
                {
                    $"{cleanNumber}@txt.att.net",        // AT&T
                    $"{cleanNumber}@vtext.com",          // Verizon
                    $"{cleanNumber}@tmomail.net",        // T-Mobile
                    $"{cleanNumber}@messaging.sprintpcs.com", // Sprint
                    $"{cleanNumber}@email.uscc.net"      // US Cellular
                };

                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email-to-SMS skipped.");
                    return false;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    // Try sending to all carriers (one will work if the number is valid)
                    bool sent = false;
                    foreach (var carrierEmail in carriers)
                    {
                        try
                        {
                            var mailMessage = new MailMessage
                            {
                                From = new MailAddress(fromEmail, "BloodConnect"),
                                Subject = "",
                                Body = message,
                                IsBodyHtml = false
                            };
                            mailMessage.To.Add(carrierEmail);

                            await client.SendMailAsync(mailMessage);
                            sent = true;
                            _logger.LogInformation($"SMS sent via Email-to-SMS to {phoneNumber} (carrier: {carrierEmail})");
                            break; // Success, no need to try other carriers
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug($"Failed to send to {carrierEmail}: {ex.Message}");
                            // Continue to next carrier
                        }
                    }

                    return sent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS via Email-to-SMS to {phoneNumber}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS using AWS SNS (FREE: First 100 SMS/month, then ~$0.00645 per SMS)
        /// Requires: dotnet add package AWSSDK.SimpleNotificationService
        /// </summary>
        private async Task<bool> SendSmsViaAwsSnsAsync(string phoneNumber, string message)
        {
            try
            {
                // Uncomment and install package: dotnet add package AWSSDK.SimpleNotificationService
                /*
                var accessKeyId = _configuration["SmsSettings:AwsAccessKeyId"];
                var secretAccessKey = _configuration["SmsSettings:AwsSecretAccessKey"];
                var region = _configuration["SmsSettings:AwsRegion"] ?? "us-east-1";

                if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
                {
                    _logger.LogWarning("AWS SNS credentials not configured.");
                    return false;
                }

                var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKeyId, secretAccessKey);
                var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient(credentials, Amazon.RegionEndpoint.GetBySystemName(region));
                
                var request = new Amazon.SimpleNotificationService.Model.PublishRequest
                {
                    PhoneNumber = phoneNumber,
                    Message = message
                };

                var response = await snsClient.PublishAsync(request);
                _logger.LogInformation($"SMS sent via AWS SNS to {phoneNumber}. MessageId: {response.MessageId}");
                return true;
                */
                
                _logger.LogWarning("AWS SNS not implemented. Install AWSSDK.SimpleNotificationService package and uncomment the code.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS via AWS SNS to {phoneNumber}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS using Twilio (FREE: $15.50 trial credit ~1,550 SMS)
        /// Requires: dotnet add package Twilio
        /// </summary>
        private async Task<bool> SendSmsViaTwilioAsync(string phoneNumber, string message)
        {
            try
            {
                // Uncomment and install package: dotnet add package Twilio
                /*
                var accountSid = _configuration["SmsSettings:TwilioAccountSid"];
                var authToken = _configuration["SmsSettings:TwilioAuthToken"];
                var twilioPhoneNumber = _configuration["SmsSettings:TwilioPhoneNumber"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(twilioPhoneNumber))
                {
                    _logger.LogWarning("Twilio credentials not configured.");
                    return false;
                }

                Twilio.TwiML.Rest.TwilioRestClient.Init(accountSid, authToken);
                var result = await Twilio.Rest.Api.V2010.Account.MessageResource.CreateAsync(
                    to: new Twilio.Types.PhoneNumber(phoneNumber),
                    from: new Twilio.Types.PhoneNumber(twilioPhoneNumber),
                    body: message
                );

                _logger.LogInformation($"SMS sent via Twilio to {phoneNumber}. SID: {result.Sid}");
                return true;
                */
                
                _logger.LogWarning("Twilio not implemented. Install Twilio package and uncomment the code.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS via Twilio to {phoneNumber}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS using Vonage (FREE: €2 credit ~20-30 SMS)
        /// Requires: dotnet add package Vonage
        /// </summary>
        private async Task<bool> SendSmsViaVonageAsync(string phoneNumber, string message)
        {
            try
            {
                // Uncomment and install package: dotnet add package Vonage
                /*
                var apiKey = _configuration["SmsSettings:VonageApiKey"];
                var apiSecret = _configuration["SmsSettings:VonageApiSecret"];
                var fromNumber = _configuration["SmsSettings:VonageFromNumber"] ?? "Vonage";

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    _logger.LogWarning("Vonage credentials not configured.");
                    return false;
                }

                var credentials = Vonage.Request.Credentials.FromApiKeyAndSecret(apiKey, apiSecret);
                var client = new Vonage.Sms.SmsClient(credentials);
                
                var request = new Vonage.Messaging.SendSmsRequest
                {
                    To = phoneNumber,
                    From = fromNumber,
                    Text = message
                };

                var response = await client.SendAnSmsAsync(request);
                _logger.LogInformation($"SMS sent via Vonage to {phoneNumber}. MessageId: {response.Messages[0].MessageId}");
                return true;
                */
                
                _logger.LogWarning("Vonage not implemented. Install Vonage package and uncomment the code.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS via Vonage to {phoneNumber}");
                return false;
            }
        }

        /// <summary>
        /// Sends SMS using TextLocal (FREE: 100 SMS/month for India)
        /// Uses HTTP API, no package needed
        /// </summary>
        private async Task<bool> SendSmsViaTextLocalAsync(string phoneNumber, string message)
        {
            try
            {
                var apiKey = _configuration["SmsSettings:TextLocalApiKey"];
                var sender = _configuration["SmsSettings:TextLocalSender"] ?? "TXTLCL";

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("TextLocal API key not configured.");
                    return false;
                }

                using var httpClient = new HttpClient();
                var url = "https://api.textlocal.in/send/";
                
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("apikey", apiKey),
                    new("numbers", phoneNumber),
                    new("message", message),
                    new("sender", sender)
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent via TextLocal to {phoneNumber}");
                    return true;
                }
                else
                {
                    _logger.LogError($"TextLocal API error: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS via TextLocal to {phoneNumber}");
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


# Notification Setup Guide

This guide explains how to configure email and SMS notifications for the BloodConnect system.

## 📧 Email Configuration

### Option 1: Gmail (Recommended for Development)

1. **Enable 2-Step Verification** on your Gmail account:
   - Go to https://myaccount.google.com/security
   - Enable "2-Step Verification"

2. **Generate an App Password**:
   - Go to https://myaccount.google.com/apppasswords
   - Select "Mail" and "Other (Custom name)"
   - Enter "BloodConnect" as the name
   - Click "Generate"
   - Copy the 16-character password (you'll use this in appsettings.json)

3. **Update `appsettings.json`**:
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-16-char-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "BloodConnect"
}
```

### Option 2: Other SMTP Providers

#### Outlook/Hotmail:
```json
"EmailSettings": {
  "SmtpHost": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SmtpUsername": "your-email@outlook.com",
  "SmtpPassword": "your-password",
  "FromEmail": "your-email@outlook.com",
  "FromName": "BloodConnect"
}
```

#### Yahoo:
```json
"EmailSettings": {
  "SmtpHost": "smtp.mail.yahoo.com",
  "SmtpPort": "587",
  "SmtpUsername": "your-email@yahoo.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "your-email@yahoo.com",
  "FromName": "BloodConnect"
}
```

#### Custom SMTP Server:
```json
"EmailSettings": {
  "SmtpHost": "smtp.yourdomain.com",
  "SmtpPort": "587",
  "SmtpUsername": "noreply@yourdomain.com",
  "SmtpPassword": "your-password",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "BloodConnect"
}
```

## 📱 SMS Configuration

### Free SMS Options (Recommended)

Here are several **FREE** alternatives to Twilio for sending SMS:

---

### Option 1: AWS SNS (Amazon Simple Notification Service) - **FREE TIER**

**Free Tier**: First 100 SMS messages per month are FREE (then ~$0.00645 per SMS)

#### Setup Steps:

1. **Create AWS Account** (if you don't have one):
   - Go to https://aws.amazon.com/
   - Sign up for free account (requires credit card but won't charge for free tier)

2. **Get AWS Credentials**:
   - Go to AWS Console → IAM → Users → Create User
   - Attach policy: `AmazonSNSFullAccess`
   - Create Access Key ID and Secret Access Key

3. **Install AWS SDK**:
   ```bash
   dotnet add package AWSSDK.SimpleNotificationService
   ```

4. **Update `appsettings.json`**:
   ```json
   "SmsSettings": {
     "Enabled": true,
     "Provider": "AWSSNS",
     "AwsAccessKeyId": "your_access_key_id",
     "AwsSecretAccessKey": "your_secret_access_key",
     "AwsRegion": "us-east-1"
   }
   ```

---

### Option 2: Email-to-SMS Gateway - **100% FREE**

**Free**: Completely free, but less reliable (carrier-dependent)

This method uses carrier email-to-SMS gateways. Each carrier has an email address format that forwards to SMS.

#### Common Carrier Email-to-SMS Formats:
- **AT&T**: `{number}@txt.att.net` (e.g., `1234567890@txt.att.net`)
- **Verizon**: `{number}@vtext.com`
- **T-Mobile**: `{number}@tmomail.net`
- **Sprint**: `{number}@messaging.sprintpcs.com`
- **US Cellular**: `{number}@email.uscc.net`

#### Setup Steps:

1. **No package installation needed** - uses built-in .NET email

2. **Update `appsettings.json`**:
   ```json
   "SmsSettings": {
     "Enabled": true,
     "Provider": "EmailToSms",
     "UseEmailToSms": true
   }
   ```

**Note**: You'll need to detect the carrier or try multiple gateways. This method is free but unreliable.

---

### Option 3: Vonage (formerly Nexmo) - **FREE CREDITS**

**Free Tier**: €2.00 free credit when you sign up (approximately 20-30 SMS messages)

#### Setup Steps:

1. **Create Vonage Account**:
   - Go to https://www.vonage.com/communications-apis/
   - Sign up for free account
   - Verify your phone number

2. **Get API Credentials**:
   - Go to Dashboard → Settings → API Credentials
   - Copy API Key and API Secret

3. **Install Vonage Package**:
   ```bash
   dotnet add package Vonage
   ```

4. **Update `appsettings.json`**:
   ```json
   "SmsSettings": {
     "Enabled": true,
     "Provider": "Vonage",
     "VonageApiKey": "your_api_key",
     "VonageApiSecret": "your_api_secret",
     "VonageFromNumber": "Vonage"
   }
   ```

---

### Option 4: TextLocal - **FREE TIER**

**Free Tier**: 100 free SMS per month (India), or low-cost international SMS

#### Setup Steps:

1. **Create TextLocal Account**:
   - Go to https://www.textlocal.in/
   - Sign up for free account

2. **Get API Key**:
   - Go to Dashboard → API → Get API Key

3. **Install HTTP Client** (built-in, no package needed)

4. **Update `appsettings.json`**:
   ```json
   "SmsSettings": {
     "Enabled": true,
     "Provider": "TextLocal",
     "TextLocalApiKey": "your_api_key",
     "TextLocalSender": "TXTLCL"
   }
   ```

---

### Option 5: Twilio (Original Option)

**Free Trial**: $15.50 credit (approximately 1,550 SMS messages)

#### Setup Steps:

1. **Create a Twilio Account**:
   - Sign up at https://www.twilio.com/try-twilio
   - Verify your phone number
   - Get a Twilio phone number (free trial includes $15.50 credit)

2. **Get Your Twilio Credentials**:
   - Go to https://console.twilio.com/
   - Find your **Account SID** and **Auth Token** on the dashboard
   - Copy your Twilio phone number (format: +1234567890)

3. **Install Twilio Package**:
```bash
dotnet add package Twilio
```

4. **Update `appsettings.json`**:
```json
"SmsSettings": {
  "Enabled": true,
  "Provider": "Twilio",
  "TwilioAccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "TwilioAuthToken": "your_auth_token_here",
  "TwilioPhoneNumber": "+1234567890"
}
```

---

### Comparison Table

| Provider | Free Tier | Reliability | Setup Difficulty | Best For |
|----------|-----------|-------------|------------------|----------|
| **AWS SNS** | 100 SMS/month | ⭐⭐⭐⭐⭐ | Medium | Production apps |
| **Email-to-SMS** | Unlimited | ⭐⭐ | Easy | Development/testing |
| **Vonage** | €2 credit (~20-30 SMS) | ⭐⭐⭐⭐ | Easy | Small projects |
| **TextLocal** | 100 SMS/month (India) | ⭐⭐⭐⭐ | Easy | India-based apps |
| **Twilio** | $15.50 credit (~1,550 SMS) | ⭐⭐⭐⭐⭐ | Easy | All use cases |

---

### Recommendation

- **For Development/Testing**: Use **Email-to-SMS** (completely free, easy setup)
- **For Production (Low Volume)**: Use **AWS SNS** (100 free SMS/month, very reliable)
- **For Production (Higher Volume)**: Use **Twilio** (best reliability, good free trial)

### Next Steps

After choosing a provider, you'll need to update the `NotificationService.cs` to implement the chosen provider. See the implementation examples in the code comments.

## 🔒 Security Best Practices

1. **Never commit credentials to Git**:
   - Add `appsettings.json` to `.gitignore` (or use `appsettings.Development.json` for local settings)
   - Use environment variables or Azure Key Vault in production

2. **Use App Passwords**:
   - Don't use your main email password
   - Generate app-specific passwords for better security

3. **Environment Variables** (Alternative):
   ```bash
   export EmailSettings__SmtpUsername="your-email@gmail.com"
   export EmailSettings__SmtpPassword="your-app-password"
   ```

## ✅ Testing

After configuration:

1. **Test Email**: Create a blood donation request in the admin panel. Check if matching donors receive emails.

2. **Test SMS**: Check application logs for SMS messages. With Twilio configured, SMS will be sent to matching donors.

3. **Check Logs**: Monitor the application logs for notification success/failure messages.

## 🐛 Troubleshooting

### Email Not Sending:
- Verify SMTP credentials are correct
- Check firewall/network allows SMTP port 587
- For Gmail: Ensure "Less secure app access" is enabled OR use App Password
- Check spam folder

### SMS Not Sending:
- Verify Twilio credentials are correct
- Ensure Twilio account has credits
- Check phone number format (must include country code, e.g., +961)
- Review Twilio console logs for errors

## 📝 Notes

- Email notifications are sent automatically when a request is created
- SMS notifications require Twilio package installation and configuration
- Notifications are sent asynchronously and won't block request creation
- Only matching donors (same blood type + location, available + healthy) receive notifications


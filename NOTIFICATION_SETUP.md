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

## 📱 SMS Configuration (Twilio)

### Step 1: Create a Twilio Account

1. Sign up at https://www.twilio.com/try-twilio
2. Verify your phone number
3. Get a Twilio phone number (free trial includes $15.50 credit)

### Step 2: Get Your Twilio Credentials

1. Go to https://console.twilio.com/
2. Find your **Account SID** and **Auth Token** on the dashboard
3. Copy your Twilio phone number (format: +1234567890)

### Step 3: Install Twilio Package

Run this command in your project directory:
```bash
dotnet add package Twilio
```

### Step 4: Update `appsettings.json`

```json
"SmsSettings": {
  "Enabled": true,
  "Provider": "Twilio",
  "TwilioAccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "TwilioAuthToken": "your_auth_token_here",
  "TwilioPhoneNumber": "+1234567890"
}
```

### Step 5: Update NotificationService

The SMS sending code is already prepared in `NotificationService.cs`. Once you add the Twilio package and configure the settings, SMS will work automatically.

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


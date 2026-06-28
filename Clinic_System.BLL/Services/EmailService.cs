

using MailKit.Net.Smtp; 
using MailKit.Security; 
using MimeKit; //  دا المسئول عن تكوين الرسالة زي الهيكل كدا يعني مين الراسل ومين المرسل ايه والموضوع عشان منساش بس
using MimeKit.Text;
using Microsoft.Extensions.Configuration;


namespace Clinic_System;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
   

    public EmailService(IConfiguration config)
    {
        _config = config;
       
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
       
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentNullException(nameof(toEmail), "User email cannot be empty.");

        var emailFrom = _config["EmailSettings:Email"];
        var password = _config["EmailSettings:Password"];

      
        if (string.IsNullOrWhiteSpace(emailFrom))
            throw new InvalidOperationException("EmailSettings:Email not found in configuration.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("EmailSettings:Password not found in configuration.");

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(emailFrom));
        email.To.Add(MailboxAddress.Parse(toEmail));

        
        email.Subject = subject ?? "Message from the Clinic flow";
        

        email.Body = new TextPart(TextFormat.Html) { Text = body ?? string.Empty };

        
        using var smtp = new SmtpClient();

        try
        {
            
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

           
            await smtp.AuthenticateAsync(emailFrom, password);

            
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Error while sending email: {ex.Message}");
            throw new InvalidOperationException($"Something went wrong While sending email: {ex.Message}", ex);
        }
        finally
        {
            
            if (smtp.IsConnected)
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}




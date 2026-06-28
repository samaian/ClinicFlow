using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System;

public interface IEmailService
{
    public  Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}

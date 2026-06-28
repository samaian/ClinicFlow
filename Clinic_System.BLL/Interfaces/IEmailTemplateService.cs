using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System;

public interface IEmailTemplateService
{
    Task<string> GetTemplateAsync(string templateName);
}

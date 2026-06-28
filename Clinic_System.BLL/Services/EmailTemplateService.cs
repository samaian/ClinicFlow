


namespace Clinic_System;

public class EmailTemplateService : IEmailTemplateService
{
   
    
        private readonly string _templatePath;

        public EmailTemplateService(string templatePath)
        {
            _templatePath = templatePath;
        }

        public async Task<string> GetTemplateAsync(string templateName)
    {
        var fullPath = Path.Combine(_templatePath,templateName);
        if (!File.Exists(fullPath))
        {
            return string.Empty;
        }
        return await File.ReadAllTextAsync(fullPath);
    }
}

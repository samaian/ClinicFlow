
using Microsoft.AspNetCore.Authentication;  
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
namespace Clinic_System;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUnitOfWork _unitOfWork;
    public AuthService(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor, IEmailService emailService,IEmailTemplateService emailTemplateService, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Response<string>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {

            return Response<string>.FailureResponse("failed to confirm your email, the link is missing or expired");

        }

        
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {

            return Response<string>.FailureResponse("failed to confirm your email ");
        }

       
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return Response<string>.FailureResponse("failed to confirm your email");
        }
        return Response<string>.SuccessResponse("email confirmed successfully");
    }

    public async Task<Response<string>> GoogleLoginAsync(string scheme, CancellationToken cancellationToken = default)
    {
        var context =  _httpContextAccessor.HttpContext;
        if (context is null)
        {
           return Response<string>.FailureResponse("External Sign-In failed.", new List<string> { "HttpContext is null." });
        }
        var result = await context.AuthenticateAsync("ExternalCookie");
        if(!result.Succeeded || result?.Principal is  null)
        {
            return Response<string>.FailureResponse("External Sign-In failed.",new List<string> { result?.Failure?.Message ?? "Unknown error" });
        }
        var externalUser = result.Principal;
       
        var email = externalUser.FindFirstValue(ClaimTypes.Email);
        var name = externalUser.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            return Response<string>.FailureResponse("External Sign-In failed.", new List<string> { "did not recieve Email Claim from google ." });
        }
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                UserName = email,
                FullName = name ?? email,
                EmailConfirmed = true
            };
            var creationResult = await _userManager.CreateAsync(user);
            if (!creationResult.Succeeded)
            {
                return Response<string>.FailureResponse("External Sign-In failed.", new List<string> { "Failed to create user." });
            }
       
        }
        var existingPatient = await _unitOfWork.Repository<Patient>()
    .Query()
    .AnyAsync(p => p.UserId == user.Id);

        if (!existingPatient)
        {
            var patient = new Patient
            {
                UserId = user.Id
            };

            await _unitOfWork.Repository<Patient>().AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();
        }

        var providerKey = externalUser.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingLogins = await _userManager.GetLoginsAsync(user);

        if (!existingLogins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == providerKey))
        {
            var info = new UserLoginInfo("Google", providerKey ?? string.Empty, "Google");
            await _userManager.AddLoginAsync(user, info);
        }

        if (!await _userManager.IsInRoleAsync(user ,"Patient"))
        {
            await _userManager.AddToRoleAsync(user, "Patient");
        }
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.FullName ?? "Patient"),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, "Patient"),
        new Claim("SecurityStamp", user.SecurityStamp ?? string.Empty)
    };
        var identity = new ClaimsIdentity(claims,scheme);
        var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(
              scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false
                });
        
        await context.SignOutAsync("ExternalCookie");
        context.Response.Cookies.Delete("ExternalCookie");
        return Response<string>.SuccessResponse("Login with Google successful.");
    }

    public async Task<Response<string>> LoginAsync(LoginDto dto,string scheme, CancellationToken cancellationToken = default)
    {
        
       
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            
            return Response<string>.FailureResponse("Invalid login attempt we sent an email.");
        }
        if (!user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var context = _httpContextAccessor.HttpContext;
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var confirmationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

            

            var emailBody = await _emailTemplateService.GetTemplateAsync(EmailTemplatesConstants.confirmationEmailTemplete);
            if (emailBody.IsNullOrEmpty())
            {
            }
            else
            {
                emailBody = emailBody.Replace("{{User}}", user.FullName);
                emailBody = emailBody.Replace("{{link}}", confirmationLink);
            }

           

            
            
            
                await _emailService.SendEmailAsync(user.Email!, "Email Confirmation", emailBody , cancellationToken);
            






            return Response<string>.FailureResponse("please confirm your email we sent an email");
        }
        var valid = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!valid)
        {


            return Response<string>.FailureResponse("Invalid login attempt.");
        }

        var roles = await _userManager.GetRolesAsync(user);

       
      

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
            new Claim("SecurityStamp", user.SecurityStamp ?? string.Empty)
        };
        if (roles.Any())
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }   
        }

        var identity = new ClaimsIdentity(
            claims,
           scheme
        );

        var principal = new ClaimsPrincipal(identity);

        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignInAsync(
               scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = dto.RememberMe
                });
        }
        return Response<string>.SuccessResponse("Login successful.");

    }



    public async Task LogoutAsync(string scheme, CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context == null)
            return;

        var userId = context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
               
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }

        
         await context.SignOutAsync(scheme);
        await context.SignOutAsync("ExternalCookie");
    }

    public async Task<Response<string>> RegisterAsync(RegisterDto registerDto, string returnUrl = "", CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            
            FullName = registerDto.Name,
            UserName = registerDto.Email,
            Email = registerDto.Email
        };
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);

        if (existingUser != null)
        {
            return Response<string>.FailureResponse("This email is already registered.");
        }
        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var response = Response<string>.FailureResponse("Registration failed.", result.Errors.Select(e => e.Description).ToList());
            return response;
        }

        try
        {
            var patient = new Patient
            {
                UserId = user.Id,
                MedicalHistory = registerDto.MedicalHistory
            };

            await _unitOfWork.Repository<Patient>().AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Response<string>.FailureResponse(
                "A patient profile already exists for this account.");
        }

        await _userManager.AddToRoleAsync(user, "Patient");
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        
        var context = _httpContextAccessor.HttpContext;
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var confirmationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

       
       
            var emailBody = await _emailTemplateService.GetTemplateAsync(EmailTemplatesConstants.confirmationEmailTemplete);
        if (emailBody.IsNullOrEmpty())
        {
        }
        else
        {
           emailBody = emailBody.Replace("{{User}}", user.FullName);
           emailBody = emailBody.Replace("{{link}}", confirmationLink);
        }
        
        if (!string.IsNullOrEmpty(returnUrl))
        {
            confirmationLink += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

      
        try
        {
            await _emailService.SendEmailAsync(user.Email, "Email Confirmation", emailBody , cancellationToken);
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);

            return Response<string>.FailureResponse("confirming email failed please try again later", new List<string> { ex.Message });
        }

   
        return Response<string>.SuccessResponse("Registration successful.");
    }
}

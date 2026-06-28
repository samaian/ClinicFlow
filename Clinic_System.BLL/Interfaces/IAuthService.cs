
namespace Clinic_System;

public interface IAuthService
{
    Task<Response<string>> LoginAsync(LoginDto dto,string scheme, CancellationToken cancellationToken = default);
    Task<Response<string>> RegisterAsync(RegisterDto registerDto, string returnUrl = "", CancellationToken cancellationToken = default);
    Task LogoutAsync(string scheme, CancellationToken cancellationToken = default);
    Task<Response<string>> GoogleLoginAsync(string scheme, CancellationToken cancellationToken = default);
    Task<Response<string>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default);

}

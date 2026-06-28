
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;


namespace Clinic_System;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var clientId = builder.Configuration["GoogleKeys:ClientId"];
        var clientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddIdentityCore<User>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
           //شوية كومنتات عشان منساش 
            options.Lockout.AllowedForNewUsers = true; // تفعيل الحظر لكل اليوزرز
            options.Lockout.MaxFailedAccessAttempts = 5; // لو كتب الباسورد غلط 5 مرات
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1); // احظر حسابه لمدة 1 دقيقة
        }).AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        builder.Services.AddDataProtection()
    .SetApplicationName("Clinic_System");
        builder.Services
.AddAuthentication(options =>
{
    options.DefaultScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
})
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/Denied";
             options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
             options.Events = new CookieAuthenticationEvents
             {
                 OnValidatePrincipal = async context =>
                 {
                     var userManager = context.HttpContext.RequestServices
                         .GetRequiredService<UserManager<User>>();

                     var userId = context.Principal?
                         .FindFirstValue(ClaimTypes.NameIdentifier);

                     var stampClaim = context.Principal?
                         .FindFirst("SecurityStamp")?.Value;

                     if (string.IsNullOrEmpty(userId))
                     {
                         context.RejectPrincipal();
                         await context.HttpContext.SignOutAsync();
                         return;
                     }

                     var user = await userManager.FindByIdAsync(userId);

                     if (user == null || user.SecurityStamp != stampClaim)
                     {
                         context.RejectPrincipal();
                         await context.HttpContext.SignOutAsync();
                     }
                 }
             };
         }).AddCookie("ExternalCookie", options =>
         {
             options.Cookie.Name = "ExternalAuthCookie";
             options.ExpireTimeSpan = TimeSpan.FromMinutes(5); 
         })
    .AddGoogle(options =>
    {
        options.ClientId = clientId!;
        options.ClientSecret = clientSecret!;
        // 2.بخلي هنا جوجل يسجل في الكوكي المؤقته بس مش التانية عشان التانية فيها security stamp فبتبوظ عملية التسجيل الخارجية
        options.SignInScheme = "ExternalCookie";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<IDoctorService, DoctorService>();

        StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<IDoctorService, DoctorService>();
        builder.Services.AddScoped<IScheduleService, ScheduleService>();
        builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddScoped<IDepartmentService, DepartmentService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IPatientService, PatientService>();
        builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IEmailTemplateService>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();

            var templatesPath = Path.Combine(
                env.ContentRootPath,
                "Templates"
            );

            return new EmailTemplateService(templatesPath);
        });


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await DbSeeder.SeedAsync(services);
            await DbSeeder.SeedRolesAsync(roleManager);
            var usermanager = services.GetRequiredService<UserManager<User>>();
            await DbSeeder.SeedAdminAsync(usermanager);
        }
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}

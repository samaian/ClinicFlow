# 🏥 ClinicFlow — Clinic Management System

A full-featured clinic management web application built with **ASP.NET Core MVC (.NET 10)**. Patients can book appointments, pay online, and leave reviews. Doctors manage their schedules. Admins oversee the entire system from a centralized dashboard.

---

## ✨ Features

- **Patient**: Browse doctors by specialty, book appointments, pay online via Stripe, leave reviews
- **Doctor**: Manage schedule, view upcoming appointments, update profile
- **Admin**: Full dashboard to manage users, departments, specialties, and appointments
- **Authentication**: Email/password login with Google OAuth 2.0 support
- **Email Notifications**: Confirmation emails sent via MailKit/SMTP with custom HTML templates

---

## 🛠️ Tech Stack

| Category | Technology |
|---|---|
| Language | C# (.NET 10) |
| Framework | ASP.NET Core MVC |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core (Code-First + Migrations) |
| Authentication | ASP.NET Core Identity + Cookie Auth + Google OAuth 2.0 |
| Payments | Stripe |
| Email | MailKit / MimeKit (SMTP) |
| Frontend | Razor Views + Tailwind CSS + DaisyUI |
| Design Patterns | Repository, Unit of Work, Service Layer, DI, DTO |
| Build Tools | npm, PostCSS, Autoprefixer |

---

## 🏗️ Architecture

The project follows a clean **3-Layer Architecture** across 3 separate projects:

```
Clinic_System.PL   →  Presentation Layer  (Controllers, Views, ViewModels)
Clinic_System.BLL  →  Business Logic Layer (Services, Interfaces, DTOs)
Clinic_System.DAL  →  Data Access Layer   (Entities, Repositories, DbContext)
```

- **DAL**: Uses the **Repository Pattern** (`IGenericRepository<T>`) and **Unit of Work** (`IUnitOfWork`) — no Controller or Service ever talks to the database directly.
- **BLL**: Every business operation has its own Interface + Service (e.g. `IAppointmentService`). DTOs prevent raw entities from leaking to the UI.
- **PL**: Handles HTTP requests/responses only. Uses Razor Views for server-side rendering.

---

## 🔐 Security

- Role-based access control: **Admin / Doctor / Patient**
- Cookie authentication with **sliding expiration** (7-day lifetime)
- **Security Stamp Validation**: password changes instantly invalidate all active sessions
- **Google OAuth 2.0** via a separate external cookie flow
- Account lockout after 5 failed login attempts
- Email confirmation required before first login
- Data Protection API for encrypting sensitive data

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server)
- [Node.js + npm](https://nodejs.org/) (for Tailwind CSS build)

### Setup

1. **Clone the repo**
   ```bash
   git clone https://github.com/your-username/ClinicFlow.git
   cd ClinicFlow
   ```

2. **Configure secrets**

   Copy `appsettings.example.json` to `appsettings.json` and fill in your values:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your-sql-server-connection-string"
     },
     "Authentication:Google:ClientId": "your-google-client-id",
     "Authentication:Google:ClientSecret": "your-google-client-secret",
     "Stripe:SecretKey": "your-stripe-secret-key",
     "EmailSettings:Password": "your-email-password"
   }
   ```
   > ⚠️ Never commit real credentials to the repo. Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables in production.

3. **Apply database migrations**
   ```bash
   cd Clinic_System.PL
   dotnet ef database update
   ```

4. **Run the app**
   ```bash
   dotnet run
   ```

---

## 📁 Project Structure

```
ClinicFlow/
├── Clinic_System.DAL/         # Data Access Layer
│   ├── Data/                  # AppDbContext
│   ├── Entities/              # Domain models (User, Doctor, Appointment...)
│   ├── Repositories/          # Generic repository + Unit of Work
│   └── Migrations/
├── Clinic_System.BLL/         # Business Logic Layer
│   ├── Services/              # Service interfaces + implementations
│   ├── Dtos/                  # Data Transfer Objects
│   └── Helpers/
└── Clinic_System.PL/          # Presentation Layer
    ├── Controllers/
    ├── Views/
    ├── ViewModels/
    └── Templates/             # HTML email templates
```

---

## 🎨 UI

- **Tailwind CSS** + **DaisyUI** with multiple theme support (`winter`, `night`, `dracula`)
- Glassmorphism effects on navbar and cards
- Scroll progress bar and smooth scrolling
- Primary font: **Inter** (Google Fonts)

---

## 📄 License

This project is for educational purposes.

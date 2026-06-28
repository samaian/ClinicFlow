using Clinic_System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;


using Microsoft.EntityFrameworkCore;

namespace Clinic_System;

public static class DbSeeder
{


    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
    }

    public static async Task SeedAdminAsync(UserManager<User> userManager)
    {
        var adminEmail = "admin@test.com";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            var user = new User
            {
                FullName = "Admin User",
                UserName = adminEmail.Split('@')[0],
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Admin@123");

            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, "Admin");
        }
    }






   
        public static async Task SeedAsync(
            IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // ===========================
            // Roles
            // ===========================
            string[] roles = { "Admin", "Doctor", "Patient" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ===========================
            // Admin
            // ===========================
            if (!userManager.Users.Any(u => u.Email == "admin@clinic.com"))
            {
                var admin = new User
                {
                    UserName = "admin@clinic.com",
                    Email = "admin@clinic.com",
                    FullName = "System Admin",
                    Age = 30,
                    IsActive = true,
                    DateOfBirth = new DateTime(1995, 1, 1),
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ===========================
            // Addresses
            // ===========================
            if (!context.Addresses.Any())
            {
                var addresses = Enumerable.Range(1, 8)
                    .Select(i => new Address
                    {
                        Area = $"Area {i}",
                        City = "Cairo"
                    });

                await context.Addresses.AddRangeAsync(addresses);
                await context.SaveChangesAsync();
            }

            // ===========================
            // Clinics
            // ===========================
            if (!context.Clinics.Any())
            {
                var clinics = Enumerable.Range(1, 8)
                    .Select(i => new SmartClinic
                    {
                        Name = $"Smart Clinic {i}",
                        AddressId = i
                    });

                await context.Clinics.AddRangeAsync(clinics);
                await context.SaveChangesAsync();
            }

            // ===========================
            // Specialties
            // ===========================
            if (!context.Specialties.Any())
            {
                var specialties = new List<Specialty>
            {
                new() { Name="Cardiology", Description="Heart Specialist" },
                new() { Name="Dermatology", Description="Skin Specialist" },
                new() { Name="Neurology", Description="Brain Specialist" },
                new() { Name="Orthopedics", Description="Bone Specialist" },
                new() { Name="Pediatrics", Description="Children Specialist" },
                new() { Name="Dentistry", Description="Dental Care" },
                new() { Name="ENT", Description="Ear Nose Throat" },
                new() { Name="Ophthalmology", Description="Eye Specialist" }
            };

                await context.Specialties.AddRangeAsync(specialties);
                await context.SaveChangesAsync();
            }

            // ===========================
            // Departments
            // ===========================
            if (!context.Departments.Any())
            {
                var departments = Enumerable.Range(1, 8)
                    .Select(i => new Department
                    {
                        SpecialtyId = i,
                        ClinicId = i
                    });

                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            // ===========================
            // Doctor Users + Doctors
            // ===========================
            if (!context.Doctors.Any())
            {
                for (int i = 1; i <= 8; i++)
                {
                    var user = new User
                    {
                        UserName = $"doctor{i}@mail.com",
                        Email = $"doctor{i}@mail.com",
                        FullName = $"Doctor {i}",
                        Age = 35 + i,
                        DateOfBirth = DateTime.UtcNow.AddYears(-(35 + i)),
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, "Doctor@123");
                    await userManager.AddToRoleAsync(user, "Doctor");

                    context.Doctors.Add(new Doctor
                    {
                        UserId = user.Id,
                        DepartmentId = i,
                        DoctorSpecialization = $"Specialist {i}",
                        ExperienceYears = 5 + i,
                        ConsultationFee = 200 + (i * 50),
                        Bio = $"Doctor Bio {i}",
                        Rate = 4.5m
                    });
                }

                await context.SaveChangesAsync();
            }

            // ===========================
            // Patient Users + Patients
            // ===========================
            if (!context.Patients.Any())
            {
                for (int i = 1; i <= 8; i++)
                {
                    var user = new User
                    {
                        UserName = $"patient{i}@mail.com",
                        Email = $"patient{i}@mail.com",
                        FullName = $"Patient {i}",
                        Age = 20 + i,
                        DateOfBirth = DateTime.UtcNow.AddYears(-(20 + i)),
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, "Patient@123");
                    await userManager.AddToRoleAsync(user, "Patient");

                    context.Patients.Add(new Patient
                    {
                        UserId = user.Id,
                        MedicalHistory = $"History {i}",
                        LastDiagnosis = $"Diagnosis {i}"
                    });
                }

                await context.SaveChangesAsync();
            }

        // ===========================
        // Schedules
        // ===========================
        if (!context.Schedules.Any())
        {
            var doctors = await context.Doctors.ToListAsync();

            foreach (var doctor in doctors)
            {
                // 16 schedules لكل دكتور
                for (int i = 0; i < 16; i++)
                {
                    context.Schedules.Add(new Schedule
                    {
                        DoctorId = doctor.Id,
                        Day = DateTime.Today.AddDays(i + 1),
                        StartTime = DateTime.Today.AddDays(i + 1).AddHours(9),
                        EndTime = DateTime.Today.AddDays(i + 1).AddHours(10),
                        ScheduleStatus = ScheduleStatus.Avaliable
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        // ===========================
        // Appointments
        // ===========================
        if (!context.Appointments.Any())
        {
            var doctors = await context.Doctors.ToListAsync();
            var patients = await context.Patients.ToListAsync();

            foreach (var doctor in doctors)
            {
                var doctorSchedules = await context.Schedules
                    .Where(s => s.DoctorId == doctor.Id)
                    .OrderBy(s => s.Id)
                    .Take(8) // أول 8 مواعيد هتكون محجوزة
                    .ToListAsync();

                for (int i = 0; i < doctorSchedules.Count; i++)
                {
                    var patient = patients[i % patients.Count];

                    context.Appointments.Add(new Appointment
                    {
                        PatientId = patient.Id,
                        DoctorId = doctor.Id,
                        ScheduleId = doctorSchedules[i].Id,
                        Status = AppointmentStatus.Completed,
                        IsPaid = true
                    });
                }
            }


            await context.SaveChangesAsync();
        }

        // ===========================
        // Payments
        // ===========================
        if (!context.Payments.Any())
            {
                var appointments = await context.Appointments.ToListAsync();

                foreach (var appointment in appointments)
                {
                    context.Payments.Add(new Payment
                    {
                        AppointmentId = appointment.Id,
                        Amount = 500,
                        Currency = "USD",
                        PaymentMethod = "Stripe",
                        Status = PaymentStatus.Completed,
                        PaidAt = DateTime.UtcNow,
                        TransactionReference = Guid.NewGuid().ToString()
                    });
                }

                await context.SaveChangesAsync();
            }

            // ===========================
            // Reviews
            // ===========================
            if (!context.Reviews.Any())
            {
                var patients = await context.Patients.ToListAsync();
                var doctors = await context.Doctors.ToListAsync();

                for (int i = 0; i < 8; i++)
                {
                    context.Reviews.Add(new Review
                    {
                        PatientId = patients[i].Id,
                        DoctorProfileId = doctors[i].Id,
                        Rating = 5,
                        Comment = $"Excellent Doctor {i + 1}"
                    });
                }

                await context.SaveChangesAsync();
            }

            // ===========================
            // Cancellation Requests
            // ===========================
            if (!context.CancellationRequests.Any())
            {
                var appointments = await context.Appointments.ToListAsync();
                var patientUsers = await userManager.GetUsersInRoleAsync("Patient");

                for (int i = 0; i < 8; i++)
                {
                    context.CancellationRequests.Add(new CancellationRequest
                    {
                        AppointmentId = appointments[i].Id,
                        RequestedByUserId = patientUsers[i].Id,
                        Reason = "Need another appointment",
                        Status = CancellationRequestStatus.Pending
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
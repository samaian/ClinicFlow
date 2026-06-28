using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clinic_System
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Schedule)
            .WithOne(s => s.Appointment)
            .HasForeignKey<Appointment>(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<AppointmentRescheduleRequest>()
            //    .HasOne(r => r.Appointment)
            //    .WithMany(a => a.RescheduleRequests)
            //    .HasForeignKey(r => r.AppointmentId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.ScheduleId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithOne(a => a.Payment)
                .HasForeignKey<Payment>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.Rate)
                .HasPrecision(3, 2);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.ConsultationFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<Doctor>(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasOne(d => d.User)
                .WithOne(u => u.PatientProfile)
                .HasForeignKey<Patient>(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>()
               .HasOne(d => d.Specialty);

            modelBuilder.Entity<Review>()
                 .HasOne(r => r.Patient)
                 .WithMany(p => p.Reviews)
                 .HasForeignKey(r => r.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Review>()
                 .HasOne(r => r.DoctorProfile)
                 .WithMany(d => d.Reviews)
                 .HasForeignKey(r => r.DoctorProfileId)
                  .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Specialty>()
                .Property(s => s.Description)
                .HasDefaultValue<string>("None");

               
                
        }



        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<SmartClinic> Clinics { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CancellationRequest> CancellationRequests { get; set; }

      
    }
}

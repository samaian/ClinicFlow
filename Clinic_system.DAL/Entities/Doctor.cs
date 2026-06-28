


namespace Clinic_System;

    public class Doctor : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public User User { get; set; } = null!;

        public string DoctorSpecialization { get; set; } = string.Empty;

        public int ExperienceYears { get; set; }

        public decimal ConsultationFee { get; set; }

        public string? Bio { get; set; }

        public decimal Rate { get; set; }

        public int DepartmentId { get; set; }

        public Department Department { get; set; } = null!;

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
        public ICollection<Review> Reviews{ get; set; }
            = new List<Review>();
            
    }

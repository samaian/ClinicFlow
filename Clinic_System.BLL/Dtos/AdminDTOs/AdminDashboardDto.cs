namespace Clinic_System;

public class AdminDashboardDto
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }

    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }

    public List<RecentAppointmentDto> RecentAppointments { get; set; } = new();
    public List<RecentPaymentDto> RecentPayments { get; set; } = new();
    public List<TopDoctorDto> TopDoctors { get; set; } = new();

    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
}

public class RecentAppointmentDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool IsPaid { get; set; }
}

public class RecentPaymentDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentStatus? Status { get; set; }
}

public class TopDoctorDto
{
    public int DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public double AverageRating { get; set; }
    public decimal Revenue { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int AppointmentCount { get; set; }
}

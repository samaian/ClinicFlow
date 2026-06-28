using Clinic_System;
namespace Clinic_System;

public class DoctorDashboardDto
{
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }

    public int TotalPatients { get; set; }

    public decimal TotalRevenue { get; set; }

    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }

    public List<RecentAppointmentDto> RecentAppointments { get; set; } = [];

    public List<RecentReviewDto> RecentReviews { get; set; } = [];
}
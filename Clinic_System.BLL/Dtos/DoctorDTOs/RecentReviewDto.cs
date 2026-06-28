public class RecentReviewDto
{
    public string PatientName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
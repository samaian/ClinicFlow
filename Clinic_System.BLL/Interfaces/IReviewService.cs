

namespace Clinic_System;

public interface IReviewService
{
    Task<Response<bool>> AddReviewAsync(CreateReviewDto dto, string userId, CancellationToken cancellationToken);
    Task<Response<PagedResult<ReviewDto>>> GetDoctorReviewsAsync(int doctorProfileId, PaginationParameters parameters, CancellationToken cancellationToken = default);
}

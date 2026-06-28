using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<bool>> AddReviewAsync(
        CreateReviewDto dto, string userId, CancellationToken cancellationToken)
    {
        if (dto == null)
            return Response<bool>.FailureResponse("Review data is required.");

        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (dto.Rating < 1 || dto.Rating > 5)
            return Response<bool>.FailureResponse("Rating must be between 1 and 5.");

        if (string.IsNullOrWhiteSpace(dto.Comment) || dto.Comment.Trim().Length < 5)
            return Response<bool>.FailureResponse("Comment must be at least 5 characters.");

        try
        {
            var patient = await _unitOfWork.Repository<Patient>()
                .Query()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (patient == null)
                return Response<bool>.FailureResponse("Patient profile not found.");

            var hasAppointment = await _unitOfWork.Repository<Appointment>()
                .Query()
                .AnyAsync(a => a.PatientId == patient.Id
                            && a.DoctorId == dto.DoctorId
                            && !a.IsDeleted, cancellationToken);

            if (!hasAppointment)
                return Response<bool>.FailureResponse("You can only review doctors you have had an appointment with.");

            var alreadyReviewed = await _unitOfWork.Repository<Review>()
                .Query()
                .AnyAsync(r => r.PatientId == patient.Id
                            && r.DoctorProfileId == dto.DoctorId, cancellationToken);

            if (alreadyReviewed)
                return Response<bool>.FailureResponse("You have already submitted a review for this doctor.");

            var review = new Review
            {
                DoctorProfileId = dto.DoctorId,
                PatientId = patient.Id,
                Rating = dto.Rating,
                Comment = dto.Comment.Trim()
            };

            await _unitOfWork.Repository<Review>().AddAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Response<bool>.SuccessResponse(true, "Review submitted successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AddReviewAsync cancelled. UserId={UserId}", userId);
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding review by userId={UserId}", userId);
            return Response<bool>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    public async Task<Response<PagedResult<ReviewDto>>> GetDoctorReviewsAsync(
        int doctorProfileId, PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        if (doctorProfileId <= 0)
            return Response<PagedResult<ReviewDto>>.FailureResponse("Invalid doctor ID.");

        if (parameters == null || parameters.PageNumber < 1 || parameters.PageSize < 1)
            return Response<PagedResult<ReviewDto>>.FailureResponse("Invalid pagination parameters.");

        try
        {
            var query = _unitOfWork.Repository<Review>()
                .Query()
                .Include(r => r.Patient)
                    .ThenInclude(p => p.User)
                .Where(r => r.DoctorProfileId == doctorProfileId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    PatientName = r.Patient.User.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Response<PagedResult<ReviewDto>>.SuccessResponse(new PagedResult<ReviewDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetDoctorReviewsAsync cancelled. DoctorId={Id}", doctorProfileId);
            return Response<PagedResult<ReviewDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for doctorId={Id}", doctorProfileId);
            return Response<PagedResult<ReviewDto>>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }
}

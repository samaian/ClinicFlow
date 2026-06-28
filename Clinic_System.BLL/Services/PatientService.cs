using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IUnitOfWork unitOfWork, ILogger<PatientService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<Patient>> GetPatientByUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<Patient>.FailureResponse("User ID is required.");

        try
        {
            var patient = await _unitOfWork.Repository<Patient>()
                .Query()
                .Where(p => p.UserId == userId)
                .FirstOrDefaultAsync();

            if (patient is null)
                return Response<Patient>.FailureResponse("No patient found with this user ID.");

            return Response<Patient>.SuccessResponse(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching patient for userId={UserId}", userId);
            return Response<Patient>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    public async Task<Response<bool>> UpdatePatientByUserId(string userId, string newMedicalHistory)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        try
        {
            var patient = await _unitOfWork.Repository<Patient>()
                .Query()
                .Where(p => p.UserId == userId)
                .FirstOrDefaultAsync();

            if (patient is null)
                return Response<bool>.FailureResponse("Patient not found.");

            patient.MedicalHistory = newMedicalHistory?.Trim() ?? string.Empty;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = userId;

            _unitOfWork.Repository<Patient>().Update(patient);
            await _unitOfWork.SaveChangesAsync();

            return Response<bool>.SuccessResponse(true, "Medical history updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient for userId={UserId}", userId);
            return Response<bool>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }
}

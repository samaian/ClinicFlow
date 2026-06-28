
namespace Clinic_System;

public interface IPatientService
{

    public Task<Response<Patient>> GetPatientByUserId(string userId);
    public Task<Response<bool>> UpdatePatientByUserId(string userId, string NewMedicalHistory);
}

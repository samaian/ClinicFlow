using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System.BLL.Dtos.AppointmentDtos
{
    public class CancellationRequestDTOs
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? Reason { get; set; }
        public CancellationRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
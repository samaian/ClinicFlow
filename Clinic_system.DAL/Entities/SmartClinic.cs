

namespace Clinic_System;

    public class SmartClinic : BaseEntity
    {

        public string Name { get; set; } = string.Empty;

        public int AddressId { get; set; }
        public Address? ClinicAddress { get; set; }

        public List<Department> Departments { get; set; } = new List<Department>();
       
    }

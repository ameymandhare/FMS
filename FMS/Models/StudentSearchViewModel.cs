using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class StudentSearchViewModel
    {
        public IEnumerable<Class> Classes { get; set; }
        public IEnumerable<School> Schools { get; set; }
        public IEnumerable<StudentQuota> StudentQuotas { get; set; }
        public IEnumerable<AcademicYear> AdmissionAcademicYears { get; set; }
        public IEnumerable<StudentStatus> Statuses { get; set; }
        public IEnumerable<Student> Students { get; set; }

        public string RegistarttionNo { get; set; }
        public string StudentName { get; set; }
        public int SelectedClassId { get; set; }
        public int SelectedSchoolId { get; set; }
        public int SelectedStudentQuotaId { get; set; }
        public int SelectedAdmissionAcademicYearId { get; set; }
        public int SelectedStatusId { get; set; }

    }
}
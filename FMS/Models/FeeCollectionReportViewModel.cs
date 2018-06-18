using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class FeeCollectionReportViewModel
    {
        public IEnumerable<Class> Classes { get; set; }
        public IEnumerable<School> Schools { get; set; }
        public IEnumerable<StudentQuota> StudentQuotas { get; set; }
        public IEnumerable<StudentStatus> Statuses { get; set; }
        public IEnumerable<PaymentMode> PaymentModes { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? FromDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ToDate { get; set; }
        public IEnumerable<AcademicYear> FinancialYears { get; set; }
        public IEnumerable<Receipt> Receipts { get; set; }
        public bool IsFinancialPeriod { get; set; }

        public int SelectedFinancialYearId { get; set; }
        public string RegistartionNo { get; set; }
        public string StudentName { get; set; }
        public int SelectedClassId { get; set; }
        public int SelectedSchoolId { get; set; }
        public int SelectedStudentQuotaId { get; set; }
        public int SelectedStatusId { get; set; }
        public int SelectedPaymentModeId { get; set; }

    }
}
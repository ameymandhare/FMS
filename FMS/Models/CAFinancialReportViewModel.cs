using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class CAFinancialReportViewModel
    {
        public int SelectedFinancialYearId { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? FromDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ToDate { get; set; }
        public IEnumerable<AcademicYear> FinancialYears { get; set; }
        public IEnumerable<Receipt> Receipts { get; set; }
        public bool IsFinancialPeriod { get; set; }

    }
}
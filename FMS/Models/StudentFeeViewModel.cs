using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class StudentFeeViewModel
    {
        public Student Student { get; set; }
        public decimal? TotalFee { get; set; }
        public decimal? Paid { get; set; }
        public bool IsPaid { get; set; }
    }
}


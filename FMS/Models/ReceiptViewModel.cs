using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class ReceiptViewModel
    {
        public IEnumerable<StudentPayment> ReceiptParticulars { get; set; }
        public decimal ReceiptTotal { get; set; }
        public decimal Balanced { get; set; }
    }
}
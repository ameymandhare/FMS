using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class PaymentViewModel
    {
        public PaymentViewModel()
        {
            this.FeeStructures = new List<FeeStructure>();
        }
        public Student Student { get; set; }
        public int PaymentModeId { get; set; }
        public string ChequeNumber { get; set; }
        [Range(1,double.MaxValue,ErrorMessage="Amount should be greater than 0.")]
        public decimal Amount { get; set; }
        public long? ReceiptId { get; set; }
        public List<FeeStructure> FeeStructures { get; set; }
        public int StudentId { get; set; }
    }
    public class FeeStructure
    {
        public string FeeStucture { get; set; }
        public int TotalAmount { get; set; }
        public int PaidAmount { get; set; }
    }
}
using FMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FMS.Controllers
{
    [Authorize(Roles = "ReceivePayment")]
    public class PaymentController : BaseController
    {
        private FMSEntities db = new FMSEntities();


        // GET: Payment
        public ActionResult Index(int? studentId)
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetStudents(string prefix)
        {
            var names = db.Students.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)
                        && (x.StudentRegistartionNo + " " + x.StudentFirstName + " " + x.StudentMiddleName + " " + x.StudentLastName).Contains(prefix))
                        .Select(x => new { val = x.StudentId, label = x.StudentRegistartionNo + (string.IsNullOrEmpty(x.StudentRegistartionNo) ? " " : " : ") + x.StudentFirstName + " " + x.StudentMiddleName + (string.IsNullOrEmpty(x.StudentMiddleName) ? "" : " ") + x.StudentLastName }).ToList();

            return Json(names);
        }

        public ActionResult Get(int studentId)
        {
            Student student = db.Students.Find(studentId);
            if (student == null)
            {
                return HttpNotFound();
            }
            ViewBag.PaymentModeId = new SelectList(db.PaymentModes, "PaymentModeId", "PaymentMode1", 1);

            PaymentViewModel model = new PaymentViewModel();
            model.Student = student;

            foreach (var classFeeBreakup in student.Class.ClassFeeBreakups)
            {
                var feeStructure = new FeeStructure();
                feeStructure.FeeStucture = classFeeBreakup.FeeType.FeeTypeDisplayName;
                feeStructure.TotalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);

                var feePaid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                feeStructure.PaidAmount = Convert.ToInt16(feePaid);
                var currentAcademicYeaarId = db.AcademicYears.Where(x => x.IsCurrent).Select(x => x.AcademicYearId).First();
                if (student.AdmissionAcademicYearId < currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Admission Fee")
                {
                    continue;
                }
                if (student.AdmissionAcademicYearId == currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee")
                {
                    continue;
                }
                if (student.StudentQuotaId == 2 && (classFeeBreakup.FeeType.FeeTypeName == "Admission Fee" || classFeeBreakup.FeeType.FeeTypeName == "Tuition Fee" || classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee"))
                {
                    continue;
                }

                model.FeeStructures.Add(feeStructure);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Get([Bind(Include = "StudentId,PaymentModeId,CheckNumber,Amount")] PaymentViewModel model)
        {
            Student student = db.Students.Find(model.StudentId);
            if (ModelState.IsValid)
            {
                bool breakOuterLoop = false;
                decimal amountLeft = model.Amount;

                Receipt receipt = new Receipt();
                receipt.Amount = model.Amount;
                var totalPaid = student.StudentPayments.Where(x => x.IsActive == true).Select(x => x.Amount).Sum();
                var total = db.ClassQuotaTotalFees.Where(x => x.ClassId == student.ClassId && x.QuotaId == student.StudentQuotaId).Select(x => x.TotalFee).FirstOrDefault();
                receipt.Balance = total - (totalPaid + model.Amount);
                if (model.Amount > total - totalPaid)
                {
                    ModelState.AddModelError(string.Empty, "Amount should not be greater than balance amount.");
                }
                if (ModelState.IsValid)
                {
                    receipt.AcademicYearId = GlobalVariables.AcademicYearId;
                    receipt.PaymentModeId = Convert.ToByte(model.PaymentModeId);
                    receipt.ChequeNumber = model.ChequeNumber;
                    receipt.IsActive = true;
                    receipt.CreatedDate = DateTime.Now;
                    receipt.CreatedBy = GetUserId();
                    db.Receipts.Add(receipt);
                    db.SaveChanges();
                    var receiptId = receipt.ReceiptId;
                    var studentPayments = new List<StudentPayment>();

                    foreach (var classFeeBreakup in student.Class.ClassFeeBreakups.OrderBy(x => x.FeeType.Priority))
                    {
                        if (breakOuterLoop)
                        {
                            break;
                        }
                        var studentPayment = new StudentPayment();
                        studentPayment.ReceiptId = receiptId;
                        studentPayment.StudentId = student.StudentId;
                        studentPayment.ClassFeeBreakupId = classFeeBreakup.ClassFeeBreakupId;
                        studentPayment.IsActive = true;
                        studentPayment.CreatedDate = DateTime.Now;
                        studentPayment.CreatedBy = 1;
                        studentPayment.AcademicYearId = GlobalVariables.AcademicYearId;
                        //var currentAcademicYeaarId = db.AcademicYears.Where(x => x.IsCurrent).Select(x => x.AcademicYearId).First();
                        if (student.AdmissionAcademicYearId < GlobalVariables.AcademicYearId && classFeeBreakup.FeeType.FeeTypeName == "Admission Fee")
                        {
                            continue;
                        }
                        if (student.AdmissionAcademicYearId == GlobalVariables.AcademicYearId && classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee")
                        {
                            continue;
                        }
                        if (student.StudentQuotaId == 2 && (classFeeBreakup.FeeType.FeeTypeName == "Admission Fee" || classFeeBreakup.FeeType.FeeTypeName == "Tuition Fee" || classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee"))
                        {
                            continue;
                        }
                        switch (classFeeBreakup.FeeType.FeeTypeName)
                        {
                            case "Admission Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Term Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Computer Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Maintainance Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Tuition Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                        }
                    }
                    db.StudentPayments.AddRange(studentPayments);
                    Receipt rec = db.Receipts.Find(receiptId);
                    rec.ReceiptName = string.Concat("KKMS/", "2017-2018/", student.Class.ClassName, "/", student.StudentRegistartionNo, "/", receiptId.ToString());
                    db.Entry(rec).State = EntityState.Modified;


                    db.SaveChanges();

                    model.Student = student;
                    model.ReceiptId = receiptId;
                }

            }
            ViewBag.PaymentModeId = new SelectList(db.PaymentModes, "PaymentModeId", "PaymentMode1", 1);
            model.Student = student;
            foreach (var classFeeBreakup in student.Class.ClassFeeBreakups)
            {
                var feeStructure = new FeeStructure();
                feeStructure.FeeStucture = classFeeBreakup.FeeType.FeeTypeDisplayName;
                feeStructure.TotalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);

                var feePaid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                feeStructure.PaidAmount = Convert.ToInt16(feePaid);
                var currentAcademicYeaarId = db.AcademicYears.Where(x => x.IsCurrent).Select(x => x.AcademicYearId).First();
                if (student.AdmissionAcademicYearId < currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Admission Fee")
                {
                    continue;
                }
                if (student.AdmissionAcademicYearId == currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee")
                {
                    continue;
                }
                if (student.StudentQuotaId == 2 && (classFeeBreakup.FeeType.FeeTypeName == "Admission Fee" || classFeeBreakup.FeeType.FeeTypeName == "Tuition Fee" || classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee"))
                {
                    continue;
                }

                model.FeeStructures.Add(feeStructure);

                //return RedirectToAction("Index","Payment", new { studentId = student.StudentId });


            }
            return View(model);
        }

        public ActionResult Receipt(int receiptId)
        {

            var receipt = db.Receipts.Find(receiptId);
            ViewBag.PaidAmountInWords = ConvertToWords(receipt.Amount.ToString());
            ViewBag.BalanceAmountInWords = ConvertToWords(receipt.Balance.ToString());
            if (receipt == null)
            {
                return HttpNotFound();
            }
            return View(receipt);
        }

        public ActionResult MiniReceipt(int receiptId)
        {
            var receipt = db.Receipts.Find(receiptId);
            ViewBag.PaidAmountInWords = ConvertToWords(receipt.Amount.ToString());
            ViewBag.BalanceAmountInWords = ConvertToWords(receipt.Balance.ToString());
            if (receipt == null)
            {
                return HttpNotFound();
            }
            return View(receipt);
        }

        [HttpPost]
        [Authorize(Roles = "DeleteReceipt")]
        public JsonResult DeleteReceipt(int receiptId)
        {
            Receipt rec = db.Receipts.Find(receiptId);
            var studentPayments = db.StudentPayments.Where(x => x.ReceiptId == receiptId);
            foreach (var payment in studentPayments)
            {
                payment.IsActive = false;
                payment.ModifiedDate = DateTime.Now;
                payment.ModifiedBy = GetUserId();
            }
            rec.IsActive = false;
            rec.ModifiedDate = DateTime.Now;
            rec.ModifiedBy = GetUserId();
            db.Entry(rec).State = EntityState.Modified;
            db.SaveChanges();
            return Json(Status.SUCCESS);
        }

        [HttpGet]
        public ActionResult SavePaymentScipt()
        {
            List<PaymentViewModel> models = new List<PaymentViewModel>();
            /*English
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 100, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 101, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 103, Amount = 6000 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 118, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 120, Amount = 10000 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 27, Amount = 2500 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 31, Amount = 10000 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 35, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 38, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 41, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 44, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 45, Amount = 5000 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 48, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 49, Amount = 7500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 50, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 51, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 52, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 53, Amount = 5000 });

            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 57, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 59, Amount = 7000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 60, Amount = 2500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 87, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 89, Amount = 15000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 90, Amount = 8000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 92, Amount = 15000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 94, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 96, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 97, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 125, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 126, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 127, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 128, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 129, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 130, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 131, Amount = 7000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 132, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 133, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 134, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 135, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 136, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 137, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 138, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 139, Amount = 6000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 140, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 141, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 142, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 143, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 144, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 145, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 146, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 147, Amount = 11000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 148, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 149, Amount = 2500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 150, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 151, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 152, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 153, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 154, Amount = 4500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 155, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 156, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 158, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 159, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 160, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 161, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 162, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 163, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 164, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 165, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 166, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 167, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 168, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 169, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 170, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 172, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 173, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 174, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 175, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 176, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 178, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 179, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 180, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 181, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 182, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 184, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 185, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 186, Amount = 8000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 187, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 188, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 189, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 190, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 191, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 192, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 193, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 194, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 195, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 196, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 197, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 198, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 199, Amount = 1500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 200, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 201, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 202, Amount = 6000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 203, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 204, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 205, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 206, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 207, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 208, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 209, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 210, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 211, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 212, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 213, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 214, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 215, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 216, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 217, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 218, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 219, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 220, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 221, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 222, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 223, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 224, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 225, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 226, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 227, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 228, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 229, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 230, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 231, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 232, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 233, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 234, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 235, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 236, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 237, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 238, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 239, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 240, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 241, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 242, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 243, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 244, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 245, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 246, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 247, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 248, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 249, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 250, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 251, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 252, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 253, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 254, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 255, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 256, Amount = 900 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 257, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 258, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 259, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 260, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 261, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 262, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 263, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 264, Amount = 6000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 265, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 266, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 267, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 268, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 269, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 270, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 271, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 272, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 273, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 274, Amount = 4000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 275, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 276, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 277, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 278, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 279, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 280, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 281, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 282, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 283, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 284, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 285, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 286, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 287, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 288, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 289, Amount = 1500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 290, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 291, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 292, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 295, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 296, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 297, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 298, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 299, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 300, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 301, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 302, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 303, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 304, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 305, Amount = 10000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 306, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 307, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 308, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 309, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 310, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 311, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 312, Amount = 2500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 313, Amount = 4500 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 314, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 315, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 316, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 317, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 318, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 319, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 320, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 321, Amount = 3000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 322, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 323, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 324, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 325, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 326, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 327, Amount = 6000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 328, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 329, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 330, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 331, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 332, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 333, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 334, Amount = 1000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 335, Amount = 5000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 336, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 337, Amount = 0 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 338, Amount = 2000 });
            //models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 339, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 340, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 341, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 342, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 343, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 344, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 345, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 346, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 347, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 348, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 349, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 350, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 351, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 352, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 353, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 354, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 355, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 356, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 357, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 358, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 359, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 360, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 361, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 362, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 363, Amount = 4500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 364, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 365, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 366, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 367, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 368, Amount = 15000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 369, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 370, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 371, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 372, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 382, Amount = 3300 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 383, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 384, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 385, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 386, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 387, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 388, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 389, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 390, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 391, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 392, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 393, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 394, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 395, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 396, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 397, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 398, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 399, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 400, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 401, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 402, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 403, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 404, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 405, Amount = 10000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 406, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 407, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 408, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 409, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 410, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 411, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 412, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 413, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 414, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 415, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 416, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 417, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 418, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 419, Amount = 13000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 420, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 421, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 422, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 423, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 424, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 425, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 426, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 427, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 428, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 429, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 430, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 431, Amount = 8000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 432, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 433, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 434, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 435, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 436, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 437, Amount = 3600 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 438, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 439, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 440, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 441, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 442, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 443, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 444, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 445, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 446, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 447, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 448, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 449, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 450, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 451, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 452, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 453, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 454, Amount = 25000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 455, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 456, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 457, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 458, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 459, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 460, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 461, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 462, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 463, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 464, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 465, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 466, Amount = 10000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 467, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 468, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 469, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 470, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 471, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 472, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 473, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 474, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 475, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 476, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 477, Amount = 13000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 478, Amount = 8000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 479, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 480, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 481, Amount = 7500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 482, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 483, Amount = 9000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 484, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 485, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 486, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 487, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 488, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 489, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 490, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 491, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 492, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 493, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 494, Amount = 1900 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 495, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 496, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 497, Amount = 15000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 498, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 499, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 500, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 501, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 502, Amount = 9000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 503, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 504, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 505, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 506, Amount = 0 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 507, Amount = 5000 });
            */

            //Marathi

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 509, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 510, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 511, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 513, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 515, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 516, Amount = 500 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 519, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 521, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 522, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 523, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 524, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 525, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 526, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 527, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 528, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 529, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 530, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 531, Amount = 3000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 533, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 534, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 536, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 537, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 538, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 539, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 541, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 542, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 543, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 544, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 545, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 546, Amount = 3000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 548, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 549, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 550, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 551, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 552, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 553, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 555, Amount = 6500 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 557, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 558, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 559, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 560, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 561, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 563, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 564, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 565, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 566, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 567, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 568, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 569, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 570, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 571, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 572, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 573, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 574, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 575, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 576, Amount = 3000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 578, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 579, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 580, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 581, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 582, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 583, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 584, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 585, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 586, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 587, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 588, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 589, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 590, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 592, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 593, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 595, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 596, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 597, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 598, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 599, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 600, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 601, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 602, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 603, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 604, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 605, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 607, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 608, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 609, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 611, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 612, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 614, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 615, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 616, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 617, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 619, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 620, Amount = 2500 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 622, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 623, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 624, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 625, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 626, Amount = 4500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 627, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 628, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 629, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 630, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 631, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 632, Amount = 6500 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 634, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 635, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 636, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 637, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 638, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 639, Amount = 3500 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 641, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 642, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 643, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 644, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 645, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 646, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 647, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 648, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 649, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 650, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 651, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 652, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 653, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 654, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 655, Amount = 4500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 656, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 657, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 658, Amount = 6000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 659, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 660, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 661, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 662, Amount = 4000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 664, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 665, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 666, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 667, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 668, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 669, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 670, Amount = 5000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 671, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 672, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 674, Amount = 1500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 675, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 676, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 677, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 678, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 679, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 680, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 681, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 682, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 683, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 684, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 685, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 686, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 687, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 688, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 689, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 690, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 691, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 693, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 694, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 695, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 697, Amount = 6500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 698, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 699, Amount = 3500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 700, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 701, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 702, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 703, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 704, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 705, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 706, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 712, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 713, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 714, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 715, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 716, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 717, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 718, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 719, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 720, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 721, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 722, Amount = 5500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 723, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 724, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 725, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 726, Amount = 5500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 727, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 729, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 730, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 732, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 733, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 734, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 735, Amount = 4500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 736, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 737, Amount = 5500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 738, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 739, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 740, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 741, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 742, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 743, Amount = 2000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 745, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 746, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 747, Amount = 3000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 748, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 749, Amount = 1000 });

            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 751, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 752, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 753, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 754, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 755, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 756, Amount = 1000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 757, Amount = 500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 758, Amount = 2500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 759, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 760, Amount = 5500 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 761, Amount = 2000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 762, Amount = 4000 });
            models.Add(new PaymentViewModel() { PaymentModeId = 1, StudentId = 763, Amount = 1000 });

            foreach (var model in models)
            {
                InsertFeeScript(model);
            }
            return null;
        }

        private void InsertFeeScript(PaymentViewModel model)
        {
            GlobalVariables.AcademicYearId = 8;
            if (true)
            {
                bool breakOuterLoop = false;
                decimal amountLeft = model.Amount;
                Student student = db.Students.Find(model.StudentId);
                Receipt receipt = new Receipt();
                receipt.Amount = model.Amount;
                var totalPaid = student.StudentPayments.Where(x => x.IsActive == true).Select(x => x.Amount).Sum();
                var total = db.ClassQuotaTotalFees.Where(x => x.ClassId == student.ClassId && x.QuotaId == student.StudentQuotaId).Select(x => x.TotalFee).FirstOrDefault();
                receipt.Balance = total - (totalPaid + model.Amount);
                if (model.Amount > total - totalPaid)
                {
                    ModelState.AddModelError(string.Empty, "Amount should not be greater than balance amount.");
                }
                if (true)
                {
                    receipt.AcademicYearId = GlobalVariables.AcademicYearId;
                    receipt.PaymentModeId = Convert.ToByte(model.PaymentModeId);
                    receipt.ChequeNumber = model.ChequeNumber;
                    receipt.IsActive = true;
                    receipt.CreatedDate = DateTime.Now;
                    receipt.CreatedBy = 2;
                    db.Receipts.Add(receipt);
                    db.SaveChanges();
                    var receiptId = receipt.ReceiptId;
                    var studentPayments = new List<StudentPayment>();

                    foreach (var classFeeBreakup in student.Class.ClassFeeBreakups.OrderBy(x => x.FeeType.Priority))
                    {
                        if (breakOuterLoop)
                        {
                            break;
                        }
                        var studentPayment = new StudentPayment();
                        studentPayment.ReceiptId = receiptId;
                        studentPayment.StudentId = student.StudentId;
                        studentPayment.ClassFeeBreakupId = classFeeBreakup.ClassFeeBreakupId;
                        studentPayment.IsActive = true;
                        studentPayment.CreatedDate = DateTime.Now;
                        studentPayment.CreatedBy = 1;
                        studentPayment.AcademicYearId = GlobalVariables.AcademicYearId;
                        //var currentAcademicYeaarId = db.AcademicYears.Where(x => x.IsCurrent).Select(x => x.AcademicYearId).First();
                        if (student.AdmissionAcademicYearId < GlobalVariables.AcademicYearId && classFeeBreakup.FeeType.FeeTypeName == "Admission Fee")
                        {
                            continue;
                        }
                        if (student.AdmissionAcademicYearId == GlobalVariables.AcademicYearId && classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee")
                        {
                            continue;
                        }
                        if (student.StudentQuotaId == 2 && (classFeeBreakup.FeeType.FeeTypeName == "Admission Fee" || classFeeBreakup.FeeType.FeeTypeName == "Tuition Fee" || classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee"))
                        {
                            continue;
                        }
                        switch (classFeeBreakup.FeeType.FeeTypeName)
                        {
                            case "Admission Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Term Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Computer Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Maintainance Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                            case "Tuition Fee":
                                {
                                    var totalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);
                                    var paid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                                    if (paid < totalAmount)
                                    {
                                        if (amountLeft > totalAmount - paid)
                                        {
                                            studentPayment.Amount = totalAmount - paid;
                                            amountLeft = amountLeft - studentPayment.Amount;
                                        }
                                        else
                                        {
                                            studentPayment.Amount = amountLeft;
                                            breakOuterLoop = true;
                                        }
                                        studentPayments.Add(studentPayment);
                                    }

                                    break;
                                }
                        }
                    }
                    db.StudentPayments.AddRange(studentPayments);
                    Receipt rec = db.Receipts.Find(receiptId);
                    rec.ReceiptName = string.Concat("KKMS/", "2017-2018/", student.Class.ClassName, "/", student.StudentRegistartionNo, "/", receiptId.ToString());
                    db.Entry(rec).State = EntityState.Modified;


                    db.SaveChanges();

                    model.Student = student;
                    model.ReceiptId = receiptId;
                }
                ViewBag.PaymentModeId = new SelectList(db.PaymentModes, "PaymentModeId", "PaymentMode1", 1);
                foreach (var classFeeBreakup in student.Class.ClassFeeBreakups)
                {
                    var feeStructure = new FeeStructure();
                    feeStructure.FeeStucture = classFeeBreakup.FeeType.FeeTypeDisplayName;
                    feeStructure.TotalAmount = Convert.ToInt16(classFeeBreakup.TotalAmount);

                    var feePaid = student.StudentPayments.Where(x => x.ClassFeeBreakupId == classFeeBreakup.ClassFeeBreakupId && x.IsActive == true).Select(x => x.Amount).Sum();
                    feeStructure.PaidAmount = Convert.ToInt16(feePaid);
                    var currentAcademicYeaarId = db.AcademicYears.Where(x => x.IsCurrent).Select(x => x.AcademicYearId).First();
                    if (student.AdmissionAcademicYearId < currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Admission Fee")
                    {
                        continue;
                    }
                    if (student.AdmissionAcademicYearId == currentAcademicYeaarId && classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee")
                    {
                        continue;
                    }
                    if (student.StudentQuotaId == 2 && (classFeeBreakup.FeeType.FeeTypeName == "Admission Fee" || classFeeBreakup.FeeType.FeeTypeName == "Tuition Fee" || classFeeBreakup.FeeType.FeeTypeName == "Maintainance Fee"))
                    {
                        continue;
                    }

                    model.FeeStructures.Add(feeStructure);

                    //return RedirectToAction("Index","Payment", new { studentId = student.StudentId });


                }

            }

        }

        private String ConvertToWords(String numb)
        {
            String val = "", wholeNo = numb, points = "", andStr = "", pointStr = "";
            String endStr = "Only";
            try
            {
                int decimalPlace = numb.IndexOf(".");
                if (decimalPlace > 0)
                {
                    wholeNo = numb.Substring(0, decimalPlace);
                    points = numb.Substring(decimalPlace + 1);
                    if (Convert.ToInt32(points) > 0)
                    {
                        andStr = "and";// just to separate whole numbers from points/cents  
                        endStr = "Paisa " + endStr;//Cents  
                        pointStr = ConvertDecimals(points);
                    }
                }

                val = String.Format("{0} {1}{2} {3}", ConvertWholeNumber(wholeNo).Trim(), andStr, pointStr, endStr);
            }
            catch { }
            return val;
        }

        private String ConvertWholeNumber(String Number)
        {
            string word = "";
            try
            {
                bool beginsZero = false;//tests for 0XX  
                bool isDone = false;//test if already translated  
                double dblAmt = (Convert.ToDouble(Number));
                //if ((dblAmt > 0) && number.StartsWith("0"))  
                if (dblAmt > 0)
                {//test for zero or digit zero in a nuemric  
                    beginsZero = Number.StartsWith("0");

                    int numDigits = Number.Length;
                    int pos = 0;//store digit grouping  
                    String place = "";//digit grouping name:hundres,thousand,etc...  
                    switch (numDigits)
                    {
                        case 1://ones' range  

                            word = Ones(Number);
                            isDone = true;
                            break;
                        case 2://tens' range  
                            word = Tens(Number);
                            isDone = true;
                            break;
                        case 3://hundreds' range  
                            pos = (numDigits % 3) + 1;
                            place = " Hundred ";
                            break;
                        case 4://thousands' range  
                        case 5:
                        case 6:
                            pos = (numDigits % 4) + 1;
                            place = " Thousand ";
                            break;
                        case 7://millions' range  
                        case 8:
                        case 9:
                            pos = (numDigits % 7) + 1;
                            place = " Million ";
                            break;
                        case 10://Billions's range  
                        case 11:
                        case 12:

                            pos = (numDigits % 10) + 1;
                            place = " Billion ";
                            break;
                        //add extra case options for anything above Billion...  
                        default:
                            isDone = true;
                            break;
                    }
                    if (!isDone)
                    {//if transalation is not done, continue...(Recursion comes in now!!)  
                        if (Number.Substring(0, pos) != "0" && Number.Substring(pos) != "0")
                        {
                            try
                            {
                                word = ConvertWholeNumber(Number.Substring(0, pos)) + place + ConvertWholeNumber(Number.Substring(pos));
                            }
                            catch { }
                        }
                        else
                        {
                            word = ConvertWholeNumber(Number.Substring(0, pos)) + ConvertWholeNumber(Number.Substring(pos));
                        }

                        //check for trailing zeros  
                        //if (beginsZero) word = " and " + word.Trim();  
                    }
                    //ignore digit grouping names  
                    if (word.Trim().Equals(place.Trim())) word = "";
                }
            }
            catch { }
            return word.Trim();
        }

        private String Ones(String Number)
        {
            int _Number = Convert.ToInt32(Number);
            String name = "";
            switch (_Number)
            {

                case 1:
                    name = "One";
                    break;
                case 2:
                    name = "Two";
                    break;
                case 3:
                    name = "Three";
                    break;
                case 4:
                    name = "Four";
                    break;
                case 5:
                    name = "Five";
                    break;
                case 6:
                    name = "Six";
                    break;
                case 7:
                    name = "Seven";
                    break;
                case 8:
                    name = "Eight";
                    break;
                case 9:
                    name = "Nine";
                    break;
            }
            return name;
        }

        private String Tens(String Number)
        {
            int _Number = Convert.ToInt32(Number);
            String name = null;
            switch (_Number)
            {
                case 10:
                    name = "Ten";
                    break;
                case 11:
                    name = "Eleven";
                    break;
                case 12:
                    name = "Twelve";
                    break;
                case 13:
                    name = "Thirteen";
                    break;
                case 14:
                    name = "Fourteen";
                    break;
                case 15:
                    name = "Fifteen";
                    break;
                case 16:
                    name = "Sixteen";
                    break;
                case 17:
                    name = "Seventeen";
                    break;
                case 18:
                    name = "Eighteen";
                    break;
                case 19:
                    name = "Nineteen";
                    break;
                case 20:
                    name = "Twenty";
                    break;
                case 30:
                    name = "Thirty";
                    break;
                case 40:
                    name = "Fourty";
                    break;
                case 50:
                    name = "Fifty";
                    break;
                case 60:
                    name = "Sixty";
                    break;
                case 70:
                    name = "Seventy";
                    break;
                case 80:
                    name = "Eighty";
                    break;
                case 90:
                    name = "Ninety";
                    break;
                default:
                    if (_Number > 0)
                    {
                        name = Tens(Number.Substring(0, 1) + "0") + " " + Ones(Number.Substring(1));
                    }
                    break;
            }
            return name;
        }

        private String ConvertDecimals(String number)
        {
            String cd = "", digit = "", engOne = "";
            for (int i = 0; i < number.Length; i++)
            {
                digit = number[i].ToString();
                if (digit.Equals("0"))
                {
                    engOne = "Zero";
                }
                else
                {
                    engOne = Ones(digit);
                }
                cd += " " + engOne;
            }
            return cd;
        }
    }

    public enum Status
    {
        SUCCESS = 1,
        FAIL = 0
    }
}
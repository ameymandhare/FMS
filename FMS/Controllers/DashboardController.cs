using FMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FMS.Controllers
{
    [Authorize(Roles = "Dashboard")]
    public class DashboardController :BaseController
    {
        private FMSEntities db = new FMSEntities();
        private static int dashboardSchoolId;
        private static int dashboardAcademicYearId;
        // GET: Dashboard
        public ActionResult Index()
        {
            if (GlobalVariables.SchoolIds.Count()==1)
            {
                dashboardSchoolId = GlobalVariables.SchoolIds[0];
                dashboardAcademicYearId = GlobalVariables.AcademicYearId;
                return RedirectToAction("ReportDashboard");
            }

            var academicYears = db.AcademicYears.OrderByDescending(x => x.AcademicYearId).Take(2);
            var current = academicYears.Where(x => x.IsCurrent == true).FirstOrDefault();
            ViewBag.AcademicYearId = new SelectList(academicYears, "AcademicYearId", "AcademicYear1", current.AcademicYearId);
            return View();
        }

        public ActionResult SetSchool(int schoolId, byte academicYearId)
        {
            dashboardSchoolId = schoolId;
            dashboardAcademicYearId= academicYearId;
            return RedirectToAction("ReportDashboard");
        }
        
        public ActionResult ReportDashboard()
        {
            return View();
        }

        public JsonResult FeeCollectionReport()
        {
            var totalFees = (from stud in db.Students
                             join cqtf in db.ClassQuotaTotalFees
                             on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                             where stud.StatusId == 1 && cqtf.IsActive == true && stud.SchoolId == dashboardSchoolId && cqtf.AcademicYearId== dashboardAcademicYearId
                             select (decimal?)cqtf.TotalFee).Sum();


            //var ss1 = db.StudentPayments.Where(y => y.AcademicYearId == dashboardAcademicYearId && y.IsActive == true && y.Student.StatusId == 1).Select(z => z.Amount).Sum();

            var paidFees = (from stud in db.Students
                            join sp in db.StudentPayments
                            on stud.StudentId equals sp.StudentId
                            where sp.IsActive == true && stud.StatusId == 1 && sp.AcademicYearId == dashboardAcademicYearId && stud.SchoolId == dashboardSchoolId
                            select (decimal?)sp.Amount).Sum();




            var classes = db.Classes.Where(x=>x.SchoolId==dashboardSchoolId).Select(x => x.ClassDisplayName).ToList();
            var collected = new List<decimal>();
            var remaining = new List<decimal>();

            var fees = from stud in db.Students
                       join sp in db.StudentPayments
                       on stud.StudentId equals sp.StudentId
                       where sp.IsActive == true && stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && sp.AcademicYearId==dashboardAcademicYearId
                       group sp by sp.StudentId into g
                       select new
                       {
                           Paid = g.Sum(x => x.Amount),
                           StudentId = g.Key
                       };

            var students = from stud in db.Students
                           join cqtf in db.ClassQuotaTotalFees
                           on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                           join f in fees
                           on stud.StudentId equals f.StudentId into j
                           from f1 in j.DefaultIfEmpty()
                           where stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && cqtf.AcademicYearId == dashboardAcademicYearId
                           select new StudentFeeViewModel()
                           {
                               Student = stud,
                               TotalFee = cqtf.TotalFee,
                               Paid = (f1 == null) ? 0 : f1.Paid,
                           };

            foreach (var cls in db.Classes.Where(x=>x.SchoolId== dashboardSchoolId))
            {
                var studs = students.Where(x => x.Student.Class.ClassId == cls.ClassId);

                if (studs == null)
                {
                    collected.Add(0);
                    remaining.Add(0);
                }
                else
                {
                    var paidAmount = studs.Select(x => x.Paid).Sum();
                    var totalAmount = studs.Select(x => x.TotalFee).Sum();
                    var rem = (totalAmount.HasValue ? totalAmount.Value : 0) - (paidAmount.HasValue ? paidAmount.Value : 0);
                    remaining.Add(rem);
                    collected.Add(paidAmount.HasValue ? paidAmount.Value : 0);
                }
            }



            var feesSummary = new
            {
                Paid = paidFees.HasValue ? paidFees.Value : 0,
                UnPaid = totalFees - (paidFees.HasValue ? paidFees.Value : 0),
                Classes = classes,
                Collected = collected,
                Remaining = remaining
            };

            return Json(feesSummary, JsonRequestBehavior.AllowGet);
        }

        public JsonResult StudentFeeStatusReport()
        {
            var fees = from stud in db.Students
                       join cqtf in db.ClassQuotaTotalFees
                       on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                       join sp in db.StudentPayments
                       on stud.StudentId equals sp.StudentId
                       where sp.IsActive == true && stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && cqtf.AcademicYearId == dashboardAcademicYearId && sp.AcademicYearId == dashboardAcademicYearId
                       group sp by new { sp.StudentId, cqtf.TotalFee } into g
                       select new
                       {
                           Paid = g.Sum(x => x.Amount),
                           TotalFee = g.Key.TotalFee,
                           StudentId = g.Key.StudentId
                       };

            var paid = (from stud in db.Students
                        join f in fees
                        on stud.StudentId equals f.StudentId
                        where stud.StatusId == 1 && f.Paid >= f.TotalFee && stud.SchoolId == dashboardSchoolId
                        select stud.StudentId).Count();

            var totalStudents = db.Students.Where(x => x.StatusId == 1 && x.SchoolId == dashboardSchoolId).Count();


            var studentFeesSummary = new
            {
                Paid = paid,
                UnPaid = totalStudents - paid
            };

            return Json(studentFeesSummary, JsonRequestBehavior.AllowGet);
        }

        public ActionResult StudentData(bool isPaid, string searchText)
        {
            var fees = from stud in db.Students
                       join sp in db.StudentPayments
                       on stud.StudentId equals sp.StudentId
                       where sp.IsActive == true && stud.StatusId == 1 && sp.AcademicYearId == dashboardAcademicYearId && stud.SchoolId == dashboardSchoolId
                       group sp by sp.StudentId into g
                       select new
                       {
                           Paid = g.Sum(x => x.Amount),
                           StudentId = g.Key
                       };

            var students = from stud in db.Students
                           join cqtf in db.ClassQuotaTotalFees
                           on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                           join f in fees
                           on stud.StudentId equals f.StudentId into j
                           from f1 in j.DefaultIfEmpty()
                           where stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && cqtf.AcademicYearId == dashboardAcademicYearId
                           select new StudentFeeViewModel()
                           {
                               Student = stud,
                               TotalFee = cqtf.TotalFee,
                               Paid = (f1 == null) ? 0 : f1.Paid,
                               IsPaid = (f1 == null || f1.Paid < cqtf.TotalFee) ? false : true
                           };
            var result = students.Where(x => x.IsPaid == isPaid).ToList();
            return View(result);
        }

        public JsonResult FeeHeadCollectionSummary()
        {
            var fromDate = new DateTime(2017, 04, 01);
            var toDate = new DateTime(2018, 03, 31).AddSeconds(86399);


            List<DrillDown> data = new List<DrillDown>();

            foreach (var feeType in db.FeeTypes)
            {
                var amount = db.StudentPayments
                            .Where(x => x.IsActive == true 
                            && x.Student.SchoolId == dashboardSchoolId 
                            && x.ClassFeeBreakup.FeeTypeId == feeType.FeeTypeId 
                            && x.AcademicYearId == dashboardAcademicYearId
                            && x.CreatedDate >= fromDate 
                            && x.CreatedDate <= toDate)
                            .Select(x => (Decimal?)x.Amount).Sum() ?? 0;
                var feeTypeName = feeType.FeeTypeDisplayName;
                data.Add(new DrillDown { name = feeTypeName, y = amount });

            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PaymentModeCollectionSummary()
        {
            var fromDate = new DateTime(2017, 04, 01);
            var toDate = new DateTime(2018, 03, 31).AddSeconds(86399);

            var byCash = db.Receipts.Where(x => x.PaymentModeId == 1 && x.IsActive == true
                            && x.CreatedDate >= fromDate && x.CreatedDate <= toDate 
                            && x.StudentPayments.Where(y=>y.AcademicYearId== dashboardAcademicYearId && y.IsActive == true).Select(y => y.Student.SchoolId).FirstOrDefault()==dashboardSchoolId)
                            .Select(x => (Decimal?)x.Amount).Sum() ?? 0;

            var byCheque = db.Receipts.Where(x => x.PaymentModeId == 2 && x.IsActive == true
              && x.CreatedDate >= fromDate && x.CreatedDate <= toDate
              && x.StudentPayments.Where(y => y.AcademicYearId == dashboardAcademicYearId && y.IsActive==true).Select(y => y.Student.SchoolId).FirstOrDefault() == dashboardSchoolId)
              .Select(x => (Decimal?)x.Amount).Sum() ?? 0;

            var paymentModeCollectionSummary = new
            {
                Cash = byCash,
                Cheque = byCheque
            };

            return Json(paymentModeCollectionSummary, JsonRequestBehavior.AllowGet);

        }


        public ActionResult ExportStudentFeeStatus(bool isPaid, string searchText)
        {
            var data = db.Students.ToList();

            GridView gridview = new GridView();
            var fees = from stud in db.Students
                       join sp in db.StudentPayments
                       on stud.StudentId equals sp.StudentId
                       where sp.IsActive == true && stud.StatusId == 1
                       && stud.SchoolId == dashboardSchoolId
                       && sp.AcademicYearId==dashboardAcademicYearId
                       group sp by sp.StudentId into g
                       select new
                       {
                           Paid = g.Sum(x => x.Amount),
                           StudentId = g.Key
                       };

            var students = from stud in db.Students
                          join cqtf in db.ClassQuotaTotalFees
                           on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                           join f in fees
                           on stud.StudentId equals f.StudentId into j
                           from f1 in j.DefaultIfEmpty()
                           where stud.StatusId == 1
                           && stud.SchoolId == dashboardSchoolId
                           && cqtf.AcademicYearId == dashboardAcademicYearId
                           select new StudentFeeViewModel()
                           {
                               Student = stud,
                               TotalFee = cqtf.TotalFee,
                               Paid = (f1 == null) ? 0 : f1.Paid,
                               IsPaid = (f1 == null || f1.Paid < cqtf.TotalFee) ? false : true
                           };
            var result = students.Where(x => x.IsPaid == isPaid).ToList();
            var exportData = result.Select(x =>
                 new
                 {
                     StudentName = string.Concat(x.Student.StudentFirstName, " ", x.Student.StudentLastName),
                     RegistartionNumber = x.Student.StudentRegistartionNo,
                     Class = x.Student.Class.ClassDisplayName,
                     TotalFee = x.TotalFee,
                     Paid = x.Paid,
                     UnPaid = x.TotalFee - x.Paid
                 });
            gridview.DataSource = exportData.ToList();
            gridview.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename = StudentFeeStatusReport.xls");
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    gridview.RenderControl(htw);
                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
            return View();
        }

        private void GetFeeCollectionClassWise()
        {
            PaymentViewModel model = new PaymentViewModel();

            var classes = db.Classes.Select(x => x.ClassDisplayName).ToList();
            var collected = new List<decimal>();
            var remaining = new List<decimal>();

            var fees = from stud in db.Students
                       join sp in db.StudentPayments
                       on stud.StudentId equals sp.StudentId
                       where sp.IsActive == true && stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && sp.AcademicYearId == dashboardAcademicYearId
                       group sp by sp.StudentId into g
                       select new
                       {
                           Paid = g.Sum(x => x.Amount),
                           StudentId = g.Key
                       };

            var students = from stud in db.Students
                           join cqtf in db.ClassQuotaTotalFees
                           on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                           join f in fees
                           on stud.StudentId equals f.StudentId into j
                           from f1 in j.DefaultIfEmpty()
                           where stud.StatusId == 1 && stud.SchoolId == dashboardSchoolId && cqtf.AcademicYearId == dashboardAcademicYearId
                           select new StudentFeeViewModel()
                           {
                               Student = stud,
                               TotalFee = cqtf.TotalFee,
                               Paid = (f1 == null) ? 0 : f1.Paid,
                           };

            foreach (var cls in db.Classes)
            {
                var studs = students.Where(x => x.Student.Class.ClassId == cls.ClassId && x.Student.SchoolId == dashboardSchoolId);
                var paidAmount = studs.Select(x => x.Paid).Sum();
                var totalAmount = studs.Select(x => x.TotalFee).Sum();
                var rem = totalAmount - (paidAmount.HasValue ? paidAmount.Value : 0);
                remaining.Add(rem.Value);
                collected.Add(paidAmount.HasValue ? paidAmount.Value : 0);
            }
        }

    }
    public class DrillDown
    {
        public string name { get; set; }
        public decimal y { get; set; }
    }

}
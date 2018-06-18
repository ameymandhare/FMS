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
    public class ReportController :BaseController
    {
        private FMSEntities db = new FMSEntities();
        // GET: Report

        [Authorize(Roles = "CAFinancialReport")]
        public ActionResult CAFinancialReport()
        {
            //var dd = (IEnumerable<Receipt>)TempData["ExportFeeCollectionReport"];
            var fromDate = new DateTime(2017, 04, 01);
            var toDate = new DateTime(2018, 03, 31).AddSeconds(86399);

            var receipts = db.Receipts.Where(x => x.CreatedDate >= fromDate && x.CreatedDate <= toDate && x.IsActive == true);
            var cAFinancialReportViewModel = new CAFinancialReportViewModel();
            cAFinancialReportViewModel.Receipts = receipts;
            cAFinancialReportViewModel.FinancialYears = db.AcademicYears.OrderByDescending(x => x.AcademicYearId);
            cAFinancialReportViewModel.IsFinancialPeriod = true;
            TempData["ExportCAFinancialReport"] = receipts.ToList();
            return View(cAFinancialReportViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "CAFinancialReport")]
        //[ValidateAntiForgeryToken]
        public ActionResult CAFinancialReport(CAFinancialReportViewModel cAFinancialReportViewModel)
        {
            int year = DateTime.Now.Year;
            var fromDate = new DateTime(year, 04, 01);
            var toDate = new DateTime(year + 1, 03, 31);
            if (ModelState.IsValid)
            {

                if (cAFinancialReportViewModel.IsFinancialPeriod)
                {
                    switch (cAFinancialReportViewModel.SelectedFinancialYearId)
                    {
                        case 8:
                            {
                                year = 2017;
                                break;
                            }
                        case 7:
                            {
                                year = 2016;
                                break;
                            }
                        case 6:
                            {
                                year = 2015;
                                break;
                            }
                        case 5:
                            {
                                year = 2014;
                                break;
                            }
                        case 4:
                            {
                                year = 2013;
                                break;
                            }
                        case 3:
                            {
                                year = 2012;
                                break;
                            }
                        case 2:
                            {
                                year = 2011;
                                break;
                            }

                        case 1:
                            {
                                year = 2010;
                                break;
                            }
                        default:
                            {
                                year = DateTime.Now.Year;
                                break;
                            }

                    }
                    fromDate = new DateTime(year, 04, 01);
                    toDate = new DateTime(year + 1, 03, 31).AddSeconds(86399);

                }
                else if (!cAFinancialReportViewModel.IsFinancialPeriod)
                {
                    if (!cAFinancialReportViewModel.FromDate.HasValue && cAFinancialReportViewModel.ToDate.HasValue)
                    {
                        fromDate = new DateTime(2001, 04, 01);
                        toDate = cAFinancialReportViewModel.ToDate.Value.AddSeconds(86399);
                    }
                    else if (!cAFinancialReportViewModel.ToDate.HasValue && cAFinancialReportViewModel.FromDate.HasValue)
                    {
                        fromDate = cAFinancialReportViewModel.FromDate.Value;
                        toDate = DateTime.Now;
                    }
                    else if (cAFinancialReportViewModel.FromDate.HasValue && cAFinancialReportViewModel.ToDate.HasValue)
                    {
                        fromDate = cAFinancialReportViewModel.FromDate.Value;
                        toDate = cAFinancialReportViewModel.ToDate.Value.AddSeconds(86399);
                    }
                    else
                    {
                        fromDate = new DateTime(2001, 04, 01);
                        toDate = DateTime.Now;
                    }

                }

            }
            var receipts = db.Receipts.Where(x => x.CreatedDate >= fromDate && x.CreatedDate <= toDate && x.IsActive == true);
            cAFinancialReportViewModel.Receipts = receipts;
            TempData["ExportCAFinancialReport"] = receipts.ToList();
            cAFinancialReportViewModel.FinancialYears = db.AcademicYears.OrderByDescending(x => x.AcademicYearId);
            return View(cAFinancialReportViewModel);
        }

        [Authorize(Roles = "CAFinancialReport")]
        public ActionResult ExportCAFinancialReport()
        {
            // Step 1 - get the data from database
            var data = db.Students.ToList();

            // instantiate the GridView control from System.Web.UI.WebControls namespace
            // set the data source
            GridView gridview = new GridView();
            var receipts = (IEnumerable<Receipt>)TempData["ExportCAFinancialReport"];
            var exportData = receipts.Select(x =>
                 new
                 {
                     Receipt = x.ReceiptName,
                     Name = x.StudentPayments.Select(y => y.Student.StudentFirstName + " " + y.Student.StudentLastName).FirstOrDefault(),
                     Amount = x.Amount,
                     Mode = x.PaymentMode.PaymentMode1,
                     PaymentDate = x.CreatedDate.ToString("dd-MMM-yyyy")
                 });
            gridview.DataSource = exportData.ToList();
            gridview.DataBind();

            // Clear all the content from the current response
            Response.ClearContent();
            Response.Buffer = true;
            // set the header
            Response.AddHeader("content-disposition", "attachment;filename = CAFinancialReport.xls");
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            // create HtmlTextWriter object with StringWriter
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    // render the GridView to the HtmlTextWriter
                    gridview.RenderControl(htw);
                    // Output the GridView content saved into StringWriter
                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
            return View();
        }

        [Authorize(Roles = "FeeCollectionReport")]
        public ActionResult FeeCollectionReport()
        {
            var fromDate = new DateTime(2017, 04, 01);
            var toDate = new DateTime(2018, 03, 31).AddSeconds(86399);

            var receipts = db.Receipts.Where(x => x.CreatedDate >= fromDate && x.CreatedDate <= toDate && x.IsActive == true
                            && GlobalVariables.SchoolIds.Contains(x.StudentPayments.Where(y => y.IsActive == true).Select(y => y.Student.SchoolId).FirstOrDefault())
                            );

            var feeCollectionReportViewModel = new FeeCollectionReportViewModel();

            feeCollectionReportViewModel.Schools = db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            feeCollectionReportViewModel.Classes = db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            feeCollectionReportViewModel.StudentQuotas = db.StudentQuotas;
            feeCollectionReportViewModel.Statuses = db.StudentStatus1;
            feeCollectionReportViewModel.PaymentModes = db.PaymentModes;

            feeCollectionReportViewModel.Receipts = receipts;
            feeCollectionReportViewModel.FinancialYears = db.AcademicYears.OrderByDescending(x => x.AcademicYearId);
            feeCollectionReportViewModel.IsFinancialPeriod = true;
            TempData["ExportFeeCollectionReport"] = receipts.ToList();
            return View(feeCollectionReportViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "FeeCollectionReport")]
        public ActionResult FeeCollectionReport(FeeCollectionReportViewModel feeCollectionReportViewModel)
        {
            int year = DateTime.Now.Year;
            var fromDate = new DateTime(year, 04, 01);
            var toDate = new DateTime(year + 1, 03, 31);
            //if (ModelState.IsValid)
            //{

            if (feeCollectionReportViewModel.IsFinancialPeriod)
            {
                switch (feeCollectionReportViewModel.SelectedFinancialYearId)
                {
                    case 8:
                        {
                            year = 2017;
                            break;
                        }
                    case 7:
                        {
                            year = 2016;
                            break;
                        }
                    case 6:
                        {
                            year = 2015;
                            break;
                        }
                    case 5:
                        {
                            year = 2014;
                            break;
                        }
                    case 4:
                        {
                            year = 2013;
                            break;
                        }
                    case 3:
                        {
                            year = 2012;
                            break;
                        }
                    case 2:
                        {
                            year = 2011;
                            break;
                        }

                    case 1:
                        {
                            year = 2010;
                            break;
                        }
                    default:
                        {
                            year = DateTime.Now.Year;
                            break;
                        }

                }
                fromDate = new DateTime(year, 04, 01);
                toDate = new DateTime(year + 1, 03, 31).AddSeconds(86399);

            }
            else if (!feeCollectionReportViewModel.IsFinancialPeriod)
            {
                if (!feeCollectionReportViewModel.FromDate.HasValue && feeCollectionReportViewModel.ToDate.HasValue)
                {
                    fromDate = new DateTime(2001, 04, 01);
                    toDate = feeCollectionReportViewModel.ToDate.Value.AddSeconds(86399);
                }
                else if (!feeCollectionReportViewModel.ToDate.HasValue && feeCollectionReportViewModel.FromDate.HasValue)
                {
                    fromDate = feeCollectionReportViewModel.FromDate.Value;
                    toDate = DateTime.Now;
                }
                else if (feeCollectionReportViewModel.FromDate.HasValue && feeCollectionReportViewModel.ToDate.HasValue)
                {
                    fromDate = feeCollectionReportViewModel.FromDate.Value;
                    toDate = feeCollectionReportViewModel.ToDate.Value.AddSeconds(86399);
                }
                else
                {
                    fromDate = new DateTime(2001, 04, 01);
                    toDate = DateTime.Now;
                }

            }


            //var receipts = db.Receipts.Where(
            //    x => x.CreatedDate >= fromDate && x.CreatedDate <= toDate
            //    && (feeCollectionReportViewModel.SelectedClassId == 0 || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.ClassId).FirstOrDefault() == feeCollectionReportViewModel.SelectedClassId)
            //     && ((feeCollectionReportViewModel.SelectedSchoolId == 0 && GlobalVariables.SchoolIds.Contains(x.StudentPayments.Where(z=>z.IsActive==true).Select(z=>z.Student.SchoolId).FirstOrDefault())) || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.SchoolId).FirstOrDefault() == feeCollectionReportViewModel.SelectedSchoolId)
            //      && (feeCollectionReportViewModel.SelectedStatusId == 0 || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.StatusId).FirstOrDefault() == feeCollectionReportViewModel.SelectedStatusId)
            //       && (feeCollectionReportViewModel.RegistartionNo == null || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.StudentRegistartionNo).FirstOrDefault() == feeCollectionReportViewModel.RegistartionNo)
            //        && (feeCollectionReportViewModel.StudentName == null || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.StudentFirstName + " " + y.Student.StudentLastName).FirstOrDefault().Contains(feeCollectionReportViewModel.StudentName))
            //        && (feeCollectionReportViewModel.SelectedStudentQuotaId == 0 || x.StudentPayments.Where(z => z.IsActive == true).Select(y => y.Student.StudentQuotaId).FirstOrDefault()==feeCollectionReportViewModel.SelectedStudentQuotaId)
            //        && (feeCollectionReportViewModel.SelectedPaymentModeId == 0 || x.PaymentModeId == feeCollectionReportViewModel.SelectedPaymentModeId)
            //    && x.IsActive == true);

            var receipts = (from r in db.Receipts
                           join sp in db.StudentPayments
                           on r.ReceiptId equals sp.ReceiptId
                           join stud in db.Students
                           on sp.StudentId equals stud.StudentId
                           where r.IsActive == true
                           && sp.IsActive == true
                           && r.CreatedDate >= fromDate && r.CreatedDate <= toDate
                           && (feeCollectionReportViewModel.SelectedClassId == 0 || stud.ClassId == feeCollectionReportViewModel.SelectedClassId)
                           && ((feeCollectionReportViewModel.SelectedSchoolId == 0 && GlobalVariables.SchoolIds.Contains(stud.SchoolId)) || stud.SchoolId == feeCollectionReportViewModel.SelectedSchoolId)
                           && (feeCollectionReportViewModel.SelectedStatusId == 0 || stud.StatusId == feeCollectionReportViewModel.SelectedStatusId)
                           && (feeCollectionReportViewModel.RegistartionNo == null || stud.StudentRegistartionNo == feeCollectionReportViewModel.RegistartionNo)
                           && (feeCollectionReportViewModel.StudentName == null || (stud.StudentFirstName + " " + stud.StudentLastName).Contains(feeCollectionReportViewModel.StudentName))
                           && (feeCollectionReportViewModel.SelectedStudentQuotaId == 0 || stud.StudentQuotaId == feeCollectionReportViewModel.SelectedStudentQuotaId)
                           && (feeCollectionReportViewModel.SelectedPaymentModeId == 0 || r.PaymentModeId == feeCollectionReportViewModel.SelectedPaymentModeId)
                           select r).Distinct();

            feeCollectionReportViewModel.Receipts = receipts;
            TempData["ExportFeeCollectionReport"] = receipts.ToList();
            //}
            //else
            //{
            //    feeCollectionReportViewModel.Receipts = db.Receipts.Where(
            //        x => x.CreatedDate >= fromDate && x.CreatedDate <= toDate);
            //    TempData["Receipts"] = feeCollectionReportViewModel.Receipts.ToList();
            //}

            feeCollectionReportViewModel.Schools = db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            feeCollectionReportViewModel.Classes = db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            feeCollectionReportViewModel.StudentQuotas = db.StudentQuotas;
            feeCollectionReportViewModel.Statuses = db.StudentStatus1;
            feeCollectionReportViewModel.FinancialYears = db.AcademicYears.OrderByDescending(x => x.AcademicYearId);
            feeCollectionReportViewModel.PaymentModes = db.PaymentModes;

            return View(feeCollectionReportViewModel);
        }

        [Authorize(Roles = "FeeCollectionReport")]
        public ActionResult ExportFeeCollectionReport()
        {
            // Step 1 - get the data from database
            var data = db.Students.ToList();

            // instantiate the GridView control from System.Web.UI.WebControls namespace
            // set the data source
            GridView gridview = new GridView();
            var receipts = (IEnumerable<Receipt>)TempData["ExportFeeCollectionReport"];
            var exportData = receipts.Select(x =>
                 new
                 {
                     Receipt = x.ReceiptName,
                     Name = x.StudentPayments.Select(y => y.Student.StudentFirstName + " " + y.Student.StudentLastName).FirstOrDefault(),
                     Amount = x.Amount,
                     Mode = x.PaymentMode.PaymentMode1,
                     PaymentDate = x.CreatedDate.ToString("dd-MMM-yyyy")
                 });
            gridview.DataSource = exportData.ToList();
            gridview.DataBind();

            // Clear all the content from the current response
            Response.ClearContent();
            Response.Buffer = true;
            // set the header
            Response.AddHeader("content-disposition", "attachment;filename = FeeCollectionReport.xls");
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            // create HtmlTextWriter object with StringWriter
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    // render the GridView to the HtmlTextWriter
                    gridview.RenderControl(htw);
                    // Output the GridView content saved into StringWriter
                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
            return View();
        }
    }
}
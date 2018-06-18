using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FMS;
using FMS.Models;

namespace FMS.Controllers
{
    [Authorize]
    public class StudentsController :BaseController
    {
        private FMSEntities db = new FMSEntities();

        // GET: Students  
      
        public ActionResult Index()
        {
            var studentSearchViewModel = new StudentSearchViewModel();
            studentSearchViewModel.Classes = db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            studentSearchViewModel.Schools = db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            studentSearchViewModel.StudentQuotas= db.StudentQuotas;
            studentSearchViewModel.AdmissionAcademicYears = db.AcademicYears;
            studentSearchViewModel.Statuses = db.StudentStatus1;

            studentSearchViewModel.Students = db.Students.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)).Include(s => s.Class).Include(s => s.School).Include(s => s.StudentQuota).Include(s => s.AcademicYear).Include(s => s.StudentStatu);
            return View(studentSearchViewModel);
        }
        [HttpPost]
        public ActionResult Index(StudentSearchViewModel studentSearchViewModel)
        {
            studentSearchViewModel.Classes = db.Classes.Where(x=>GlobalVariables.SchoolIds.Contains(x.SchoolId));
            studentSearchViewModel.Schools = db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId));
            studentSearchViewModel.StudentQuotas = db.StudentQuotas;
            studentSearchViewModel.AdmissionAcademicYears = db.AcademicYears;
            studentSearchViewModel.Statuses = db.StudentStatus1;

            studentSearchViewModel.Students = db.Students.Where(x=> (studentSearchViewModel.SelectedClassId==0 ||x.ClassId==studentSearchViewModel.SelectedClassId)
            && ((studentSearchViewModel.SelectedSchoolId == 0 && GlobalVariables.SchoolIds.Contains(x.SchoolId))|| x.SchoolId == studentSearchViewModel.SelectedSchoolId)
            && (studentSearchViewModel.SelectedStudentQuotaId == 0 || x.StudentQuotaId == studentSearchViewModel.SelectedStudentQuotaId)
            && (studentSearchViewModel.SelectedAdmissionAcademicYearId == 0 || x.AdmissionAcademicYearId == studentSearchViewModel.SelectedAdmissionAcademicYearId)
            && (studentSearchViewModel.SelectedStatusId == 0 || x.StatusId == studentSearchViewModel.SelectedStatusId)
            && (studentSearchViewModel.RegistarttionNo == null || x.StudentRegistartionNo==studentSearchViewModel.RegistarttionNo)
            && (studentSearchViewModel.StudentName == null || (x.StudentFirstName + " " + x.StudentLastName).Contains(studentSearchViewModel.StudentName))
            )
                .Include(s => s.Class).Include(s => s.School).Include(s => s.StudentQuota).Include(s => s.AcademicYear).Include(s => s.StudentStatu);
            return View(studentSearchViewModel);
        }

        // GET: Students/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = await db.Students.FindAsync(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }

        // GET: Students/Create
        
        [Authorize(Roles = "CreateStudent")]
        public ActionResult Create()
        {
            ViewBag.ClassId = new SelectList(db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "ClassId", "ClassDisplayName");
            ViewBag.SchoolId = new SelectList(db.Schools.Where(x=>GlobalVariables.SchoolIds.Contains(x.SchoolId)), "SchoolId", "SchoolDisplayName");
            ViewBag.StudentQuotaId = new SelectList(db.StudentQuotas, "StudentQuotaId", "StudentQuotaDisplayName");
            ViewBag.AdmissionAcademicYearId = new SelectList(db.AcademicYears, "AcademicYearId", "AcademicYear1");
            ViewBag.StatusId = new SelectList(db.StudentStatus1, "StudentStatusId", "StatusDispalyName");
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "CreateStudent")]
        public ActionResult Create([Bind(Include = "StudentId,StudentRegistartionNo,ClassId,SchoolId,Gender,StudentLastName,StudentFirstName,StudentMiddleName,DOB,BirthAddress,ReligionAndCast,StudentQuotaId,ParentLastName,ParentFirstName,ParentMiddleName,AnnualIncom,RelationWithStudent,PreviousSchool,LastPassedClass,LastPassedYear,LastClassMarks,LastClassTotalMarks,LastClassPercentage,AdmissionDate,AdmissionAcademicYearId,StatusId,CurrentAddress,CantactNumbers,PermanentAddress,ParentOccuapation")] Student student)
        {
            if (ModelState.IsValid)
            {
                student.CreatedBy = GetUserId();
                student.CreatedDate = DateTime.Now;
                db.Students.Add(student);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "ClassId", "ClassDisplayName", student.ClassId);
            ViewBag.SchoolId = new SelectList(db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "SchoolId", "SchoolDisplayName", student.SchoolId);
            ViewBag.StudentQuotaId = new SelectList(db.StudentQuotas, "StudentQuotaId", "StudentQuotaDisplayName", student.StudentQuotaId);
            ViewBag.AdmissionAcademicYearId = new SelectList(db.AcademicYears, "AcademicYearId", "AcademicYear1", student.AdmissionAcademicYearId);
            ViewBag.StatusId = new SelectList(db.StudentStatus1, "StudentStatusId", "StatusDispalyName", student.StatusId);
            return View(student);
        }

        // GET: Students/Edit/5
        [Authorize(Roles = "CreateStudent")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = await db.Students.FindAsync(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClassId = new SelectList(db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "ClassId", "ClassDisplayName", student.ClassId);
            ViewBag.SchoolId = new SelectList(db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "SchoolId", "SchoolDisplayName", student.SchoolId);
            ViewBag.StudentQuotaId = new SelectList(db.StudentQuotas, "StudentQuotaId", "StudentQuotaName", student.StudentQuotaId);
            ViewBag.AdmissionAcademicYearId = new SelectList(db.AcademicYears, "AcademicYearId", "AcademicYear1", student.AdmissionAcademicYearId);
            ViewBag.StatusId = new SelectList(db.StudentStatus1, "StudentStatusId", "StatusName", student.StatusId);
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "CreateStudent")]
        public async Task<ActionResult> Edit([Bind(Include = "StudentId,StudentRegistartionNo,ClassId,SchoolId,Gender,StudentLastName,StudentFirstName,StudentMiddleName,DOB,BirthAddress,ReligionAndCast,StudentQuotaId,ParentLastName,ParentFirstName,ParentMiddleName,AnnualIncom,RelationWithStudent,PreviousSchool,LastPassedClass,LastPassedYear,LastClassMarks,LastClassTotalMarks,LastClassPercentage,AdmissionDate,AdmissionAcademicYearId,CreatedDate,StatusId,CurrentAddress,CantactNumbers,PermanentAddress,ParentOccuapation")] Student student)
        {
            if (ModelState.IsValid)
            {
                student.ModifiedBy = GetUserId();
                student.ModifiedDate = DateTime.Now;
                db.Entry(student).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ClassId = new SelectList(db.Classes.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "ClassId", "ClassDisplayName", student.ClassId);
            ViewBag.SchoolId = new SelectList(db.Schools.Where(x => GlobalVariables.SchoolIds.Contains(x.SchoolId)), "SchoolId", "SchoolDisplayName", student.SchoolId);
            ViewBag.StudentQuotaId = new SelectList(db.StudentQuotas, "StudentQuotaId", "StudentQuotaName", student.StudentQuotaId);
            ViewBag.AdmissionAcademicYearId = new SelectList(db.AcademicYears, "AcademicYearId", "AcademicYear1", student.AdmissionAcademicYearId);
            ViewBag.StatusId = new SelectList(db.StudentStatus1, "StudentStatusId", "StatusName", student.StatusId);
            return View(student);
        }

        // GET: Students/Delete/5
        [Authorize(Roles = "DeleteStudent")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = await db.Students.FindAsync(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "DeleteStudent")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Student student = await db.Students.FindAsync(id);
            student.StatusId = 2;
            db.Entry(student).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

         private decimal GetTotalFee(int studentId)
        {

            var totalFee = (from stud in db.Students
                            join cqtf in db.ClassQuotaTotalFees
                            on new { Classid = stud.ClassId, StudentQuotaId = stud.StudentQuotaId } equals new { Classid = cqtf.ClassId, StudentQuotaId = cqtf.QuotaId }
                            where stud.StudentId == studentId
                            select cqtf.TotalFee).FirstOrDefault();

            return totalFee;



        }

        private decimal GetPaidFee(int studentId)
        {
            var paidFee = (from stud in db.Students
                           join sp in db.StudentPayments
                           on stud.StudentId equals sp.StudentId
                           where sp.IsActive == true && stud.StudentId == studentId
                           group sp by sp.StudentId into g
                           select g.Sum(x => x.Amount)).FirstOrDefault();
            return paidFee;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

       
    }
}

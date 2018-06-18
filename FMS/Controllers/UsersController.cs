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
using Microsoft.AspNet.Identity;

namespace FMS.Controllers
{
    [Authorize(Roles = "CreateUser")]
    public class UsersController :BaseController
    {
        private FMSEntities db = new FMSEntities();

        // GET: Users
        public async Task<ActionResult> Index()
        {
            var users = db.FMSUsers
               .Select(x => new UserViewModel()
               {
                   UserId = x.UserId,
                   Email = x.Email,
                   CreatedBy = x.FMSUser2.Email,
                   CreatedDate = x.CreatedDate,
                   ModifiedBy = x.FMSUser3.Email,
                   ModifiedDate = x.ModifiedDate,
                   IsActive = x.IsActive,
                   UserSchools = x.UserSchools.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserSchool() { SchoolName = y.School.SchoolDisplayName }).ToList(),
                   UserRoles = x.UserRoles.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserRole() { RoleName = y.FMSRole.RoleName }).ToList(),
               });

            return View(await users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSUser fMSUser = await db.FMSUsers.FindAsync(id);
            if (fMSUser == null)
            {
                return HttpNotFound();
            }
            return View(fMSUser);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            var userViewModel = new UserViewModel();
            userViewModel.UserSchools = db.Schools.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserSchool() { SchoolId = y.SchoolId, SchoolName = y.SchoolDisplayName, IsSelected = false }).ToList();
            userViewModel.UserRoles = db.FMSRoles.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserRole() { RoleId = y.RoleId, RoleName = y.RoleName, IsSelected = false }).ToList();
            return View(userViewModel);
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "UserId,Email,UserSchools,UserRoles")] UserViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var fmsUser = new FMSUser();
                    fmsUser.Email = userViewModel.Email;
                    fmsUser.Password = userViewModel.Email;
                    fmsUser.IsActive = true;
                    fmsUser.CreatedDate = DateTime.Now;
                    fmsUser.CreatedBy = GetUserId();
                    db.FMSUsers.Add(fmsUser);
                    await db.SaveChangesAsync();
                    var userId = fmsUser.UserId;
                    var userSchools = userViewModel.UserSchools.Where(x => x.IsSelected == true).Select(x => new UserSchool()
                    {
                        UserId = userId,
                        SchoolId = x.SchoolId,
                        CreatedDate = DateTime.Now,
                        CreatedBy = GetUserId(),
                        IsActive = true
                    });

                    var userRoles = userViewModel.UserRoles.Where(x => x.IsSelected == true).Select(x => new UserRole()
                    {
                        UserId = userId,
                        RoleId = x.RoleId,
                        CreatedDate = DateTime.Now,
                        CreatedBy = GetUserId(),
                        IsActive = true
                    });

                    db.UserSchools.AddRange(userSchools);
                    db.UserRoles.AddRange(userRoles);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {

                }
            }

            return View(userViewModel);
        }

        // GET: Users/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSUser fMSUser = await db.FMSUsers.FindAsync(id);

            var user = db.FMSUsers.Where(x => x.UserId == id)
              .Select(x => new UserViewModel()
              {
                  UserId = x.UserId,
                  Email = x.Email,
                  CreatedBy = x.FMSUser2.Email,
                  CreatedDate = x.CreatedDate,
                  ModifiedBy = x.FMSUser3.Email,
                  ModifiedDate = x.ModifiedDate,
                  UserSchools = db.Schools.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserSchool() { SchoolId = y.SchoolId, SchoolName = y.SchoolDisplayName, IsSelected = x.UserSchools.Where(z => z.IsActive == true).Select(z => z.SchoolId).Contains(y.SchoolId) }).Distinct().ToList(),
                  UserRoles = db.FMSRoles.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserRole() { RoleId = y.RoleId, RoleName = y.RoleName, IsSelected = x.UserRoles.Where(z => z.IsActive == true).Select(z => z.RoleId).Contains(y.RoleId) }).Distinct().ToList(),
                  IsActive = x.IsActive
              }).FirstOrDefault();

            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "UserId,Email,UserSchools,UserRoles,IsActive")] UserViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                var fmsUser = db.FMSUsers.Find(userViewModel.UserId);
                fmsUser.Email = userViewModel.Email;
                fmsUser.IsActive = userViewModel.IsActive;
                fmsUser.ModifiedDate = DateTime.Now;
                fmsUser.ModifiedBy = GetUserId();

            var userSchools = db.UserSchools.Where(x => x.UserId == userViewModel.UserId && x.IsActive == true);

                foreach (var userSchool in userSchools)
                {
                    userSchool.IsActive = false;
                    userSchool.ModifiedDate = DateTime.Now;
                    userSchool.ModifiedBy = GetUserId();
                    db.Entry(userSchool).State = EntityState.Modified;
                }

                var userRoles = db.UserRoles.Where(x => x.UserId == userViewModel.UserId && x.IsActive == true);

                foreach (var userRole in userRoles)
                {
                    userRole.IsActive = false;
                    userRole.ModifiedDate = DateTime.Now;
                    userRole.ModifiedBy = GetUserId();
                db.Entry(userRole).State = EntityState.Modified;
                }

                db.Entry(fmsUser).State = EntityState.Modified;

                var modifiedUserSchools = userViewModel.UserSchools.Where(x => x.IsSelected == true).Select(x => new UserSchool()
                {
                    UserId = userViewModel.UserId,
                    SchoolId = x.SchoolId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = GetUserId(),
                    IsActive = true
                });

                db.UserSchools.AddRange(modifiedUserSchools);

                var modifiedUserRoles = userViewModel.UserRoles.Where(x => x.IsSelected == true).Select(x => new UserRole()
                {
                    UserId = userViewModel.UserId,
                    RoleId = x.RoleId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = GetUserId(),
                    IsActive = true
                });

                db.UserRoles.AddRange(modifiedUserRoles);


                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(userViewModel);
        }

        // GET: Users/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSUser fMSUser = await db.FMSUsers.FindAsync(id);
            if (fMSUser == null)
            {
                return HttpNotFound();
            }
            return View(fMSUser);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            FMSUser fMSUser = await db.FMSUsers.FindAsync(id);
            fMSUser.IsActive = false;
            fMSUser.ModifiedDate = DateTime.Now;
            fMSUser.ModifiedBy = GetUserId();

            var userSchools = db.UserSchools.Where(x => x.UserId == id && x.IsActive == true);
            foreach (var userSchool in userSchools)
            {
                userSchool.IsActive = false;
                userSchool.ModifiedDate = DateTime.Now;
                userSchool.ModifiedBy = GetUserId();
                db.Entry(userSchool).State = EntityState.Modified;
            }

            var userRoles = db.UserRoles.Where(x => x.UserId == id && x.IsActive == true);
            foreach (var userRole in userRoles)
            {
                userRole.IsActive = false;
                userRole.ModifiedDate = DateTime.Now;
                userRole.ModifiedBy = GetUserId();
                db.Entry(userRole).State = EntityState.Modified;
            }

            db.Entry(fMSUser).State = EntityState.Modified;

            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpPost]
        public JsonResult ResetPassword(int userId)
        {
            var fmsUser = db.FMSUsers.Where(x => x.UserId == userId).FirstOrDefault();
            fmsUser.Password = fmsUser.Email;
            fmsUser.ModifiedDate = DateTime.Now;
            fmsUser.ModifiedBy = GetUserId();
            db.Entry(fmsUser).State = EntityState.Modified;

            db.SaveChanges();
            return Json(Status.SUCCESS);
        }

    }

}

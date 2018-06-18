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
using System.Text;
using Microsoft.AspNet.Identity;

namespace FMS.Controllers
{

    [Authorize(Roles = "CreateUser")]
    public class FMSRolesController :BaseController
    {
        private FMSEntities db = new FMSEntities();

        // GET: FMSRoles
        public ActionResult Index()
        {

            var roles = db.FMSRoles.Where(x => x.IsActive == true)
               .Select(x => new RoleViewModel()
               {
                   RoleId = x.RoleId,
                   RoleName = x.RoleName,
                   CreatedBy = x.FMSUser.Email,
                   CreatedDate = x.CreatedDate,
                   ModifiedBy = x.FMSUser1.Email,
                   ModifiedDate = x.ModifiedDate,
                   RoleModules = x.RoleModules.Where(y => y.IsActive == true).Select(y => new FMS.Models.RoleModule() { ModuleName = y.Module.ModuleDisplayName }).ToList()
               }).ToList();


            return View(roles);
        }

        // GET: FMSRoles/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSRole fMSRole = await db.FMSRoles.FindAsync(id);
            if (fMSRole == null)
            {
                return HttpNotFound();
            }
            return View(fMSRole);
        }

        // GET: FMSRoles/Create
        public ActionResult Create()
        {
            var roleViewModel = new RoleViewModel();
            roleViewModel.RoleModules = db.Modules.Where(y => y.IsActive == true).Select(y => new FMS.Models.RoleModule() { ModuleId = y.ModuleId, ModuleName = y.ModuleDisplayName, IsSelected = false }).ToList();
            return View(roleViewModel);
        }

        // POST: FMSRoles/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "RoleId,RoleName,RoleModules")] RoleViewModel roleViewModel)
        {
            if (ModelState.IsValid)
            {
                var fmsRole = new FMSRole();
                fmsRole.RoleName = roleViewModel.RoleName;
                fmsRole.IsActive = true;
                fmsRole.CreatedDate = DateTime.Now;
                fmsRole.CreatedBy = GetUserId();
                db.FMSRoles.Add(fmsRole);
                await db.SaveChangesAsync();
                var roleId = fmsRole.RoleId;
                var roleModules = roleViewModel.RoleModules.Where(x => x.IsSelected == true).Select(x => new RoleModule()
                {
                    FMSRole = fmsRole,
                    RoleId = roleId,
                    ModuleId = x.ModuleId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = GetUserId(),
                IsActive = true
                });

                db.RoleModules.AddRange(roleModules);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(roleViewModel);
        }

        // GET: FMSRoles/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSRole fMSRole = await db.FMSRoles.FindAsync(id);

            var role = db.FMSRoles.Where(x => x.RoleId == id)
              .Select(x => new RoleViewModel()
              {
                  RoleId = x.RoleId,
                  RoleName = x.RoleName,
                  CreatedBy = x.FMSUser.Email,
                  CreatedDate = x.CreatedDate,
                  ModifiedDate = x.ModifiedDate,
                  RoleModules = db.Modules.Where(y => y.IsActive == true).Select(y => new FMS.Models.RoleModule() { ModuleId = y.ModuleId, ModuleName = y.ModuleDisplayName, IsSelected = x.RoleModules.Where(z => z.IsActive == true).Select(z => z.ModuleId).Contains(y.ModuleId) }).Distinct().ToList()
              }).FirstOrDefault();

            if (role == null)
            {
                return HttpNotFound();
            }
            return View(role);
        }

        // POST: FMSRoles/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "RoleId,RoleName,RoleModules")] RoleViewModel roleViewModel)
        {
            if (ModelState.IsValid)
            {
                var fmsRole = db.FMSRoles.Find(roleViewModel.RoleId);
                fmsRole.RoleName = roleViewModel.RoleName;
                fmsRole.IsActive = true;
                fmsRole.ModifiedDate = DateTime.Now;
                fmsRole.ModifiedBy = GetUserId();

                var roleModules = db.RoleModules.Where(x => x.RoleId == roleViewModel.RoleId && x.IsActive == true);
                foreach (var roleModule in roleModules)
                {
                    roleModule.IsActive = false;
                    roleModule.ModifiedDate = DateTime.Now;
                    roleModule.ModifiedBy = GetUserId();
                    db.Entry(roleModule).State = EntityState.Modified;
                }
                db.Entry(fmsRole).State = EntityState.Modified;

                var modifiedRoleModules = roleViewModel.RoleModules.Where(x => x.IsSelected == true).Select(x => new RoleModule()
                {
                    FMSRole = fmsRole,
                    RoleId = roleViewModel.RoleId,
                    ModuleId = x.ModuleId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = GetUserId(),
                    IsActive = true
                });

                db.RoleModules.AddRange(modifiedRoleModules);

                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(roleViewModel);
        }

        // GET: FMSRoles/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FMSRole fMSRole = await db.FMSRoles.FindAsync(id);
            if (fMSRole == null)
            {
                return HttpNotFound();
            }
            return View(fMSRole);
        }

        // POST: FMSRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            FMSRole fMSRole = await db.FMSRoles.FindAsync(id);
            fMSRole.IsActive = false;
            fMSRole.ModifiedDate = DateTime.Now;
            fMSRole.ModifiedBy = GetUserId();

            var roleModules = db.RoleModules.Where(x => x.RoleId == id && x.IsActive == true);
            foreach (var roleModule in roleModules)
            {
                roleModule.IsActive = false;
                roleModule.ModifiedDate = DateTime.Now;
                roleModule.ModifiedBy = GetUserId();
                db.Entry(roleModule).State = EntityState.Modified;
            }
            db.Entry(fMSRole).State = EntityState.Modified;

            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Members(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var users = db.FMSUsers.Where(x => x.UserRoles.Where(y => y.IsActive == true).Select(z => z.RoleId).Contains(id.Value))
               .Select(x => new UserViewModel()
               {
                   Email = x.Email,
                   UserSchools = x.UserSchools.Where(y => y.IsActive == true).Select(y => new FMS.Models.UserSchool() { SchoolName = y.School.SchoolDisplayName }).ToList()
               });

            return View(await users.ToListAsync());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        

        //private List<RoleModule> GetSelectedModules()
        //{

        //}
    }
}

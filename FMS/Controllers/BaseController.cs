using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FMS.Controllers
{
    public class BaseController : Controller
    {
        private FMSEntities db = new FMSEntities();
        public int GetUserId()
        {
            int userId = 0;
            var email = User.Identity.GetUserName();
            if (!string.IsNullOrEmpty(email))
                userId = db.FMSUsers.Where(x => x.Email == email).Select(x => x.UserId).FirstOrDefault();
            return userId;
        }
    }
}
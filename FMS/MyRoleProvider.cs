using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

namespace FMS
{
    public class MyRoleProvider : RoleProvider
    {
        private int _cacheTimeoutInMinute = 30;
       public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return null;
            }

            //check cache
            var cacheKey = string.Format("{0}_role", username);
            //if (HttpRuntime.Cache[cacheKey] != null)
            //{
            //    return (string[])HttpRuntime.Cache[cacheKey];
            //}
            string[] roles = new string[] { };
            using (FMSEntities db = new FMSEntities())
            {
                roles = (from m in db.Modules
                         join rm in db.RoleModules
                         on m.ModuleId equals rm.ModuleId
                         join r in db.FMSRoles
                         on rm.RoleId equals r.RoleId
                         join ur in db.UserRoles on r.RoleId equals ur.RoleId
                         join u in db.FMSUsers on ur.UserId equals u.UserId
                         where u.Email.Equals(username) && m.IsActive==true && rm.IsActive==true && m.IsActive==true && r.IsActive==true && ur.IsActive==true && u.IsActive==true
                         select m.ModuleName).ToArray<string>();
                if (roles.Count() > 0)
                {
                    //HttpRuntime.Cache.Insert(cacheKey, roles, null, DateTime.Now.AddMinutes(_cacheTimeoutInMinute), Cache.NoSlidingExpiration);

                }
            }
            return roles;
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            var userRoles = GetRolesForUser(username);
            return userRoles.Contains(roleName);
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}
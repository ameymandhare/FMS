using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

namespace MvcAuthentication
{
    public class MyRoleProvider : RoleProvider
    {
        public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
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

//if (!HttpContext.Current.User.Identity.IsAuthenticated)
//{
//    return null;
//}

////check cache
//var cacheKey = string.Format("{0}_role", username);
//if (HttpRuntime.Cache[cacheKey] != null)
//{
//    return (string[])HttpRuntime.Cache[cacheKey];
//}
//string[] roles = new string[] { };
//using (FMSEntities db = new FMSEntities())
//{
//    roles = (from a in db.FMSRoles
//             join b in db.UserRoles on a.RoleId equals b.RoleId
//             join c in db.FMSUsers on b.UserId equals c.UserId
//             where c.Email.Equals(username)
//             select a.RoleName).ToArray<string>();
//    if (roles.Count() > 0)
//    {
//        HttpRuntime.Cache.Insert(cacheKey, roles, null, DateTime.Now.AddMinutes(_cacheTimeoutInMinute), Cache.NoSlidingExpiration);

//    }
//}
//return roles;


//var userRoles = GetRolesForUser(username);
//return userRoles.Contains(roleName);
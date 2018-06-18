using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class UserViewModel
    {
        public int UserId { get; set;    }
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        //[Required]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        //[DataType(DataType.Password)]
        //[Display(Name = "Password")]
        //public string Password { get; set; }

        //[DataType(DataType.Password)]
        //[Display(Name = "Confirm password")]
        //[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        //public string ConfirmPassword { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
        [Display(Name = "Created Date")]
        public System.DateTime CreatedDate { get; set; }
        [Display(Name = "Modified By")]
        public string ModifiedBy { get; set; }
        [Display(Name = "Modified Date")]
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public List<UserSchool> UserSchools { get; set; }

        public List<UserRole> UserRoles { get; set; }

    }
    public class UserSchool
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class UserRole
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }

}
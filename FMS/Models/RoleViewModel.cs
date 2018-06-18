using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public class RoleViewModel
    {
        public RoleViewModel()
        {
            this.RoleModules = new List<RoleModule>();
        }
        public int RoleId { get; set; }
        [Required]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
        [Display(Name = "Created Date")]
        public System.DateTime CreatedDate { get; set; }
        [Display(Name = "Modified By")]
        public string ModifiedBy { get; set; }
        [Display(Name = "Modified Date")]
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public List<RoleModule> RoleModules { get; set; }
    }

    public class RoleModule
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public bool IsSelected { get; set; }
    }
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FMS
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserSchool
    {
        public int UserSchoolId { get; set; }
        public int UserId { get; set; }
        public int SchoolId { get; set; }
        public int CreatedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public Nullable<int> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public bool IsActive { get; set; }
    
        public virtual FMSUser FMSUser { get; set; }
        public virtual FMSUser FMSUser1 { get; set; }
        public virtual FMSUser FMSUser2 { get; set; }
        public virtual School School { get; set; }
    }
}

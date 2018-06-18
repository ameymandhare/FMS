using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS.Models
{
    public static class GlobalVariables
    {
        public static int[] SchoolIds
        {
            get
            {
                if (HttpContext.Current.Session["SchoolIds"] != null)
                {
                    return (int[])HttpContext.Current.Session["SchoolIds"];
                }
                else
                {
                    return new int[] { 0};
                }
            }
            set
            {
                HttpContext.Current.Session["SchoolIds"] = value;
            }
        }

        public static byte AcademicYearId
        {
            get
            {
                return Convert.ToByte(HttpContext.Current.Session["AcademicYearId"]);
            }
            set
            {
                HttpContext.Current.Session["AcademicYearId"] = value;
            }
        }

    }
}


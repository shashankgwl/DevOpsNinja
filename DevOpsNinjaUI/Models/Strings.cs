using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOpsNinjaUI.Models
{
    public static class Strings
    {
        public static string FetchLoadUnmanagedSolutions
        {
            get
            {
                return @"<fetch version='1.0' output-format='xml-platform' mapping='logical'>
<entity name=""solution"">< attribute name = ""solutionid"" />
< attribute name = ""friendlyname"" />
< attribute name = ""isapimanaged"" />
< attribute name = ""isapimanagedname"" />
< attribute name = ""uniquename"" />
< attribute name = ""ismanaged"" />
< attribute name = ""ismanagedname"" />
< attribute name = ""publisherid"" />
< attribute name = ""publisheridname"" />
< attribute name = ""solutionpackageversion"" />
< attribute name = ""version"" />
< filter type = ""and"" >
< condition attribute = ""ismanaged"" operator= ""eq"" value = ""0"" />
</ filter >< order attribute = ""createdon"" descending = ""true"" />
</ entity ></ fetch >";
            }
        }
    }
}

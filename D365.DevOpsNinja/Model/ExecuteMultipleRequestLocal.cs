using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365.DevOpsNinja.Model
{
    internal class ExecuteMultipleRequestLocal
    {
        public ExecuteMultipleRequest ExecuteMultipleRequestInner { get; set; }

        public string SolutionName { get; set; }
    }
}

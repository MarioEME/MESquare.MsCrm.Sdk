using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MESquare.MsCrm.Sdk
{
    public class AuditDataFilterEntity
    {
        public String EntityLogicalName { get; set; }
        public List<String> Attributes { get; set; }
        public Boolean AllAttributes { get; set; }
    }
}

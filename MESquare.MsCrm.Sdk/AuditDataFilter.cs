using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MESquare.MsCrm.Sdk
{
    public class AuditDataFilter
    {
        public DateTime CreatedAfter { get; set; } = DateTime.UtcNow;
        public DateTime CreatedBefore { get; set; } = DateTime.UtcNow.AddMonths(-1);
        public String Type { get; set; }
        public Guid? Author { get; set; }
        public List<AuditDataFilterEntity> Entities { get; set; }
        public int NumberOfRecords { get; set; } = 500;
        public int Take { get; set; } = 500;
        public int Skip { get; set; } = 0;
        public Guid? ObjectId { get; set; }
        public List<int> Actions { get; set; } = null;
        public List<int> ExcludeActions { get; set; } = null;
        public string PagingCookie { get; set; } = null;
        public int MaxNumberOfParallelRequests { get; set; }
        public int NumberOfRequestsPerBulkRequest { get; set; }
    }

}

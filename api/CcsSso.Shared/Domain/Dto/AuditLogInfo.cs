using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain.Dto
{    
    public class AuditLogResponseInfo
    {
        public int Id { get; set; }
        public string Event { get; set; }
        public string UserId { get; set; }
        public string Application { get; set; }
        public string ReferenceData { get; set; }
        public string IpAddress { get; set; }
        public string Device { get; set; }
        public DateTime EventTimeUtc { get; set; }       
    }
}

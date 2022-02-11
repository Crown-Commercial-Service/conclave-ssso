using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
    public class CcsServiceInfo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public string Url { get; set; }
    }

    public class ServiceProfile
    {
        public int ServiceId { get; set; }

        public string Audience { get; set; }

        public List<string> RoleKeys { get; set; }
    }

    public class CountryDetail
    {
        public int Id { get; set; }

        public string CountryName { get; set; }

        public string CountryCode { get; set; }
    }
}

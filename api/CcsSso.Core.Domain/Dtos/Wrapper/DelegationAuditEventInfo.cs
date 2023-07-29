using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.Wrapper
{
	public class DelegationAuditEventRequestInfo
	{
		public Guid GroupId { get; set; }

		public string UserName { get; set; }

		public int OrganisationId { get; set; }

		public DateTime? PreviousDelegationStartDate { get; set; }

		public DateTime? PreviousDelegationEndDate { get; set; }

		public DateTime? NewDelegationStartDate { get; set; }

		public DateTime? NewDelegationEndDate { get; set; }

		public string Roles { get; set; }

		public string EventType { get; set; }

		public DateTime ActionedOnUtc { get; set; }

		public string ActionedBy { get; set; }

		public string ActionedByUserName { get; set; }

		public string ActionedByFirstName { get; set; }

		public string ActionedByLastName { get; set; }
	}
	public class DelegationEmailNotificationLogInfo
	{
		public string UserName { get; set; }

		public int OrganisationId { get; set; }

		public DateTime? DelegationEndDate { get; set; }

	}
}
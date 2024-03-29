﻿using CcsSso.Core.DbModel.Constants;
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

		public string CiiOrganisationId { get; set; }

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

		public string CiiOrganisationId { get; set; }

		public DateTime? DelegationEndDate { get; set; }

	}

	public class WrapperOrganisationAuditInfo
	{
    public OrgAutoValidationStatus Status { get; set; }

    public string SchemeIdentifier { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string OrganisationId { get; set; }
  }
  public class WrapperOrganisationAuditEventInfo
  {

    public string SchemeIdentifier { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid GroupId { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }

    public string Event { get; set; }

    public string Roles { get; set; }

    public string OrganisationId { get; set; }
  }


}
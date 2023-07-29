using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.Wrapper
{
	public class WrapperOrganisationProfileResponseInfo
	{
		public OrganisationIdentifier Identifier { get; set; }

		public List<OrganisationIdentifier> AdditionalIdentifiers { get; set; }

		public OrganisationAddressResponse Address { get; set; }

		// Commented since this is still not available from CII service
		//public OrganisationContactPoint ContactPoint { get; set; }

		public OrganisationDetail Detail { get; set; }
	}


	public class OrganisationIdentifier
	{
		public string Id { get; set; }

		public string LegalName { get; set; }

		public string Uri { get; set; }

		public string Scheme { get; set; }
	}

	public class OrganisationAddressResponse
	{
		public string StreetAddress { get; set; }

		public string Locality { get; set; }

		public string Region { get; set; }

		public string PostalCode { get; set; }

		public string CountryCode { get; set; }

		public string CountryName { get; set; }
	}
	public class OrganisationDetail
	{
		public string OrganisationId { get; set; }

		public string CreationDate { get; set; }

		public string BusinessType { get; set; }

		public int SupplierBuyerType { get; set; }

		public bool IsSme { get; set; }

		public bool IsVcse { get; set; }

		public bool RightToBuy { get; set; }

		public bool IsActive { get; set; }

		public string DomainName { get; set; }
	}

	public class OrganisationListResponseInfo : PaginationInfo
	{
		public List<OrganisationData> OrgList { get; set; }
	}

	public class OrganisationData
	{
		public int OrganisationId { get; set; }

		public string CiiOrganisationId { get; set; }

		public string OrganisationUri { get; set; }

		public string LegalName { get; set; }

		public bool? RightToBuy { get; set; }

		public string BusinessType { get; set; }

		public int SupplierBuyerType { get; set; }

		public int PartyId { get; set; }

		public DateTime CreatedOnUtc { get; set; }

	}
	public class PaginationInfo
	{
		public int CurrentPage { get; set; }

		public int PageCount { get; set; }

		public int RowCount { get; set; }
	}
	public class OrganisationFilterCriteria
	{
		[FromQuery(Name = "organisation-name")]
		public string? OrganisationName { get; set; } = null;

		[FromQuery(Name = "exact-match-name")]
		public bool IsExactMatchName { get; set; } = false;

		[FromQuery(Name = "include-all")]
		public bool IncludeAll { get; set; } = false;

		[FromQuery(Name = "organisation-ids")]
		public string? OrganisationIds { get; set; }

		[FromQuery(Name = "is-match-name")]
		public bool IsMatchName { get; set; } = false;

		[FromQuery(Name = "start-date")]
		public string? StartDate { get; set; }

		[FromQuery(Name = "end-date")]
		public string? EndDate { get; set; }

		[FromQuery(Name = "until-date-time")]
		public string? UntilDateTime { get; set; }
	}
}

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
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
		public DateTime? StartDate { get; set; }

		[FromQuery(Name = "end-date")]
		public DateTime? EndDate { get; set; }

		[FromQuery(Name = "until-date-time")]
		public DateTime? UntilDateTime { get; set; }
	}
}

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class ResultSetCriteria
  {
		[JsonProperty(PropertyName = "page-size")]
		[FromQuery(Name = "page-size")]
		public int PageSize { get; set; }

		[JsonProperty(PropertyName = "current-page")]
		[FromQuery(Name = "current-page")]
		public int CurrentPage { get; set; }

		[JsonProperty(PropertyName = "is-pagination")]
		[FromQuery(Name = "is-pagination")]
		public bool IsPagination { get; set; } = false;
	}	
}
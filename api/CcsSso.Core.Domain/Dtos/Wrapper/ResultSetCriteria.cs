using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.Wrapper
{
  public class ResultSetCriteria
  {
		[JsonProperty(PropertyName = "page-size")]
		public int PageSize { get; set; }

		[JsonProperty(PropertyName = "current-page")]
		public int CurrentPage { get; set; }

		[JsonProperty(PropertyName = "is-pagination")]
		public bool IsPagination { get; set; } = false;
	}	
}
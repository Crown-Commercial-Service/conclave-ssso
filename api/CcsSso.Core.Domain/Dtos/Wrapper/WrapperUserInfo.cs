using CcsSso.Core.DbModel.Constants;
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
	public class DelegationUserDto
	{
		public int UserId { get; set; }

		public string UserName { get; set; }

		public int OrganisationId { get; set; }

		public DateTime? DelegationLinkExpiryOnUtc { get; set; }

		public DateTime? DelegationStartDate { get; set; }

		public DateTime? DelegationEndDate { get; set; }

	}

	
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.JobScheduler.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Notify.Client;
using OrganisationDetail = CcsSso.Core.JobScheduler.Model.OrganisationDetail;

namespace CcsSso.Core.JobScheduler.Services
{
  public class AutoValidationService : IAutoValidationService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AutoValidationService> _logger;
    private readonly IEmailProviderService _emaillProviderService;
    private bool reportingMode;

    public AutoValidationService(IHttpClientFactory httpClientFactory, AppSettings appSettings, 
      ILogger<AutoValidationService> logger, IEmailProviderService emaillProviderService)
    {
      _httpClientFactory = httpClientFactory;
      _appSettings = appSettings;
      _logger = logger;
      _emaillProviderService = emaillProviderService;
      reportingMode = false;
    }

    public async Task PerformJobAsync(List<OrganisationDetail> organisations)
    {
      if (organisations != null)
      {
        reportingMode = _appSettings.OrgAutoValidationOneTimeJob.ReportingMode;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _appSettings.WrapperApiSettings.ApiKey);
        client.BaseAddress = new Uri(_appSettings.WrapperApiSettings.Url);

        var jobReport = new List<OrganisationLogDetail>();

        foreach (var orgDetail in organisations)
        {
          try
          {
            HttpResponseMessage response = await AutoValidation(client, orgDetail);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
              _logger.LogInformation($"Org autovalidation success " + orgDetail.CiiOrganisationId);

              AddtoLogger(jobReport, orgDetail, null, "auto validation lookup api call success");


              var responseContent = await response.Content.ReadAsStringAsync();
              var responseObject = JObject.Parse(responseContent).ToObject<Tuple<bool, string>>();


              if (reportingMode)
              {
                AddtoLogger(jobReport, orgDetail, responseObject, "auto validation look up result, Reporting Mode. So skipping role assignment");
                continue;
              }

              AddtoLogger(jobReport, orgDetail, responseObject, "auto validation look up result");

              HttpResponseMessage roleResponse = await AutoValidationAddRoles(client, orgDetail, responseObject.Item1, responseObject.Item2);
              

              if (roleResponse.StatusCode == System.Net.HttpStatusCode.OK)
              {
                AddtoLogger(jobReport, orgDetail, responseObject, "auto validation Role assignment success");

                _logger.LogInformation($"Org autovalidation role assignment succeeded " + orgDetail.CiiOrganisationId);
              }
              else
              {
                AddtoLogger(jobReport, orgDetail, responseObject, $"auto validation Role assignment Failed, Message-{response.Content.ReadAsStringAsync().Result}");

                _logger.LogInformation($"Org autovalidation role assignment Failed " + orgDetail.CiiOrganisationId);
              }
            }
            else
            {
              AddtoLogger(jobReport, orgDetail, null, $"auto validation lookup api call failed. Message-{response.Content.ReadAsStringAsync().Result}");


              _logger.LogInformation($"Org autovalidation falied " + orgDetail.CiiOrganisationId);
            }
          }
          catch (Exception e)
          {
            AddtoLogger(jobReport, orgDetail, null, $"Error while processing auto validation. Exception { JsonConvert.SerializeObject(e)}");
            _logger.LogError($"Org autovalidation error " + JsonConvert.SerializeObject(e));
          }
        }
        _logger.LogInformation($"Finished one time validation job for all the organisations ");

        if (jobReport.Count>0)
        {
          _logger.LogInformation(JsonConvert.SerializeObject(jobReport));
        }

        try
        {
          var email = _appSettings.OrgAutoValidationOneTimeJob.LogReportEmailId;
          if (!string.IsNullOrEmpty(email) && jobReport.Count()>0)
          {
            await SendLogDetailEmailAsync(jobReport, new List<string> { email});
          }
        }
        catch (Exception)
        {
          _logger.LogInformation($"Error while sending log email");
        }
      }
    }

    private async Task SendLogDetailEmailAsync(List<OrganisationLogDetail> logs, List<string> toEmails)
    {
      byte[] documentContents = ConvertToCsv(logs);


      var data = new Dictionary<string, dynamic>
      {
        { "link_to_file", NotificationClient.PrepareUpload(documentContents, true) }

      };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in toEmails)
      {
        var emailTempalteId = _appSettings.OrgAutoValidationOneTimeJobEmail.FailedAutoValidationNotificationTemplateId;
        var emailInfo = GetEmailInfo(toEmail, emailTempalteId, data); 

        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }

      await Task.WhenAll(emailTaskList);
    }

    private EmailInfo GetEmailInfo(string toEmail, string templateId, Dictionary<string, dynamic> data)
    {
      var emailInfo = new EmailInfo
      {
        To = toEmail,
        TemplateId = templateId,
        BodyContent = data
      };

      return emailInfo;
    }

    private static byte[] ConvertToCsv(List<OrganisationLogDetail> organizations)
    {
      var csv = new StringBuilder();

      var csvHeader = string.Format("{0},{1},{2},{3},{4}", "Organisation Id", "Organisation Name", "Administrator Email", "Autovalidation Status","Information", "SupplierBuyerType", "RightToBuy", "Date and Time");
      csv.AppendLine(csvHeader);

      foreach (var item in organizations)
      {
        var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", item.Id, item.LegalName, item.AdminEmail, item.AutovalidationStatus,item.Information,item.SupplierBuyerType,item.RightToBuy, item.DateTime);
        csv.AppendLine(newLine);
      }

      byte[] documentContents = Encoding.ASCII.GetBytes(csv.ToString());
      return documentContents;
    }

    private void AddtoLogger(List<OrganisationLogDetail> jobReport, OrganisationDetail orgDetail, Tuple<bool, string> responseObject,string information)
    {
      jobReport.Add(new OrganisationLogDetail()
      {
        CiiOrganisationId = orgDetail.CiiOrganisationId,
        Id = orgDetail.Id,
        LegalName = orgDetail.LegalName,
        AdminEmail = responseObject?.Item2,
        AutovalidationStatus = responseObject?.Item1.ToString(),
        Information=information,
        SupplierBuyerType =orgDetail.SupplierBuyerType,
        RightToBuy =orgDetail.RightToBuy,
        DateTime = ConvertToGmtDateTime((DateTime)orgDetail.CreatedOnUtc)
      });
    }

    private async Task<HttpResponseMessage> AutoValidationAddRoles(HttpClient client, OrganisationDetail orgDetail, bool isDomainValid, string oldAdminEmailId)
    {
      _logger.LogInformation($"Autovalidation CiiOrganisationId:- {orgDetail.CiiOrganisationId} ");

      var url = "/organisations/" + orgDetail.CiiOrganisationId + "/autovalidationjob/roles";

      AutoValidationOneTimeJobDetails details = new AutoValidationOneTimeJobDetails()
      {
        AdminEmailId = oldAdminEmailId,
        IsFromBackgroundJob = true,
        CompanyHouseId = "",
        isDomainValid = isDomainValid
      };

      HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(details, new JsonSerializerSettings
      { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

      var response = await client.PostAsync(url, httpContent);
      return response;

    }

    private async Task<HttpResponseMessage> AutoValidation(HttpClient client, OrganisationDetail orgDetail)
    {
      _logger.LogInformation($"Autovalidation CiiOrganisationId:- {orgDetail.CiiOrganisationId} ");

      var url = "/organisations/" + orgDetail.CiiOrganisationId + "/autovalidationjob";
      var response = await client.PostAsync(url, null);
      return response;
    }

    private string ConvertToGmtDateTime(DateTime dateTime)
    {
      var easternZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
      var convertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, easternZone);
      return convertedDateTime.ToString("dd/MM/yyyy hh:mm", CultureInfo.InvariantCulture);
    }
  }
}

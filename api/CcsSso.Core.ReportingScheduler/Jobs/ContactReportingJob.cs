using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql.Internal;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks.Dataflow;
using System.Web;
using ContactResponseInfo = CcsSso.Shared.Domain.Dto.ContactResponseInfo;


namespace CcsSso.Core.ReportingScheduler.Jobs
{
  public class ContactReportingJob : BackgroundService
  {
    private readonly ILogger<ContactReportingJob> _logger;
    private readonly AppSettings _appSettings;
    private readonly IDataContext _dataContext;
    private readonly IDateTimeService _dataTimeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICSVConverter _csvConverter;
    private readonly IFileUploadToCloud _fileUploadToCloud;

    public ContactReportingJob(IServiceScopeFactory factory, ILogger<ContactReportingJob> logger,
       IDateTimeService dataTimeService, AppSettings appSettings, IHttpClientFactory httpClientFactory,
       ICSVConverter csvConverter, IFileUploadToCloud fileUploadToCloud)
    {
      _logger = logger;
      _appSettings = appSettings;
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _dataTimeService = dataTimeService;
      _httpClientFactory = httpClientFactory;
      _csvConverter = csvConverter;
      _fileUploadToCloud = fileUploadToCloud;

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        int interval = _appSettings.ScheduleJobSettings.ContactReportingJobScheduleInMinutes * 60000; //15000;

        // Get the Organisation Contact Details, User Contact Details, Site Contact Details in a excel sheet differentiate based on the org,user,site

        _logger.LogInformation("Contact Reporting Job  running at: {time}", DateTimeOffset.Now);
        await PerformJob();

        _logger.LogInformation("Contact Reporting Job  finished at: {time}", DateTimeOffset.Now);
        _logger.LogInformation("");

        await Task.Delay(interval, stoppingToken);

      }
    }

    private async Task PerformJob()
    {
      try
      {
        var totalNumberOfItemsDuringThisSchedule = 0;

        var listOfAllModifiedContact = new List<ContactModel>();

        var orgContacts = await GetModifiedContactIds();
        orgContacts.ForEach(x => listOfAllModifiedContact.Add(new ContactModel { ContactType = "ORG", DetectedContact = x }));

        var userContacts = await GetModifiedUserContactIds();
        userContacts.ForEach(x => listOfAllModifiedContact.Add(new ContactModel { ContactType = "USER", DetectedContact = x }));

        var siteContacts = await GetModifiedSiteContactIds();
        siteContacts.ForEach(x => listOfAllModifiedContact.Add(new ContactModel { ContactType = "SITE", DetectedContact = x }));



        if (listOfAllModifiedContact == null || listOfAllModifiedContact.Count() == 0)
        {
          _logger.LogInformation("No contact found");
          return;
        }

        _logger.LogInformation($"Total number of Contacts => {listOfAllModifiedContact.Count()}");

        // spliting the jobs
        int size = _appSettings.MaxNumbeOfRecordInAReport;
        _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
        var index = 0;

        var contactDetailList = new List<ContactDetailModel>();

        foreach (var eachModifiedOrg in listOfAllModifiedContact)
        {
          index++;
          _logger.LogInformation($"trying to get contact details of {index}");

          try
          {

            try
            {
              _logger.LogInformation("Calling wrapper API to get contact Details");
              var client = _httpClientFactory.CreateClient("ContactWrapperApi");

              switch (eachModifiedOrg.ContactType)
              {
                case "ORG":                  
                  var contactDetails = await GetOrgContactDetails((ModifiedOrgContactInfo)eachModifiedOrg.DetectedContact, client);
                  if (contactDetails != null)
                  {
                    contactDetails.contactType = "organisation";
                    contactDetailList.Add(new ContactDetailModel { ContactType = eachModifiedOrg.ContactType, ContactDetail = contactDetails });
                  }
                  break;
                case "USER":                  
                  var contactUserDetails = await GetUserContactDetails((ModifiedUserContactInfo)eachModifiedOrg.DetectedContact, client);
                  if (contactUserDetails != null)
                  {
                    contactUserDetails.contactType = "user";
                    contactDetailList.Add(new ContactDetailModel { ContactType = eachModifiedOrg.ContactType, ContactDetail = contactUserDetails });
                  }
                  break;
                case "SITE":
                  var contactSiteDetails = await GetSiteContactDetails((ModifiedSiteContactInfo)eachModifiedOrg.DetectedContact, client);
                  if (contactSiteDetails != null)
                  {
                    contactSiteDetails.contactType = "site";
                    contactDetailList.Add(new ContactDetailModel { ContactType = eachModifiedOrg.ContactType, ContactDetail = contactSiteDetails });
                  }
                  break;

                default:
                  break;
              }

            }
            catch (Exception ex)
            {

              _logger.LogError($" XXXXXXXXXXXX Failed to retrieve contact details from Wrapper Api. Contact Type ={eachModifiedOrg.ContactType}; Contact Detail= {eachModifiedOrg.DetectedContact} and Message - {ex.Message} XXXXXXXXXXXX");
            }

            if (listOfAllModifiedContact.Count != index && contactDetailList.Count < size)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Contact in this Batch => {contactDetailList.Count()}");
            totalNumberOfItemsDuringThisSchedule += contactDetailList.Count();


            _logger.LogInformation("After calling the wrapper API to get Contact Details");


            var fileByteArrayOrg = _csvConverter.ConvertToCSV(contactDetailList.Where(x => x.ContactType == "ORG").Select(x => (ContactOrgResponseInfo)x.ContactDetail).ToList(), "contact-org");
            var fileByteArrayUser = _csvConverter.ConvertToCSV(contactDetailList.Where(x => x.ContactType == "USER").Select(x => (ContactUserResponseInfo)x.ContactDetail).ToList(), "contact-user");
            var fileByteArraySite = _csvConverter.ConvertToCSV(contactDetailList.Where(x => x.ContactType == "SITE").Select(x => (ContactSiteResponseInfo)x.ContactDetail).ToList(), "contact-site");
            byte[] fileByteArray = fileByteArrayOrg.Concat(fileByteArrayUser).Concat(fileByteArraySite).ToArray();

            //using (MemoryStream memStream = new MemoryStream(fileByteArray))
            //{
            //  File.WriteAllBytes($"contact_{new Random().Next()}.csv", fileByteArray);
            //}

            if (_appSettings.WriteCSVDataInLog)
            {
              try
              {
                MemoryStream ms = new MemoryStream(fileByteArray);
                StreamReader reader = new StreamReader(ms);
                string csvData = reader.ReadToEnd();
                _logger.LogInformation("CSV Data as follows");
                _logger.LogInformation(csvData);
                _logger.LogInformation("");
              }
              catch (Exception ex)
              {
                _logger.LogWarning($"It is temporary logging to check the csv data which through exception -{ex.Message}");
              }
            }

            _logger.LogInformation("After converting the list of contact object into CSV format and returned byte Array");

            AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "Contact");
            _logger.LogInformation("After Transfered the files to Azure Blob");

            if (result.responseStatus)
            {
              _logger.LogInformation($"****************** Successfully transfered file. FileName - {result.responseFileName} ******************");
              _logger.LogInformation("");
            }
            else
            {
              _logger.LogError($" XXXXXXXXXXXX Failed to transfer. Message - {result.responseMessage} XXXXXXXXXXXX");
              _logger.LogError($"Failed to transfer. File Name - {result.responseFileName}");
              _logger.LogInformation("");

            }

          }
          catch (Exception)
          {

            _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of contact in this set {contactDetailList.Count()} XXXXXXXXXXXX");
            _logger.LogError("");

          }
          contactDetailList.Clear();
          await Task.Delay(5000);

        }

        _logger.LogInformation($"Total number of contacts exported during this schedule => {totalNumberOfItemsDuringThisSchedule}");
      }
      catch (Exception ex)
      {

        _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
        _logger.LogError("");
      }
    }




    private async Task<ContactSiteResponseInfo?> GetSiteContactDetails(ModifiedSiteContactInfo eachModifiedContact, HttpClient client)
    {

      string url = $"contact-service/organisations/{eachModifiedContact.CiiOrgId}/sites/{eachModifiedContact.SiteContactId}/contacts/{eachModifiedContact.ContactPointId}";

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.ContactPointId}");
        return JsonConvert.DeserializeObject<ContactSiteResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.ContactPointId}");
        return null;
      }
    }

    private async Task<ContactUserResponseInfo?> GetUserContactDetails(ModifiedUserContactInfo eachModifiedContact, HttpClient client)
    {

      string url = $"contact-service/user/contacts/{eachModifiedContact.ContactPointId}?user-id={HttpUtility.UrlEncode(eachModifiedContact.UserName)}";

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.ContactPointId}");
        return JsonConvert.DeserializeObject<ContactUserResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.ContactPointId}");
        return null;
      }
    }



    private async Task<ContactOrgResponseInfo?> GetOrgContactDetails(ModifiedOrgContactInfo eachModifiedContact, HttpClient client)
    {
      string url = $"contact-service/organisations/{eachModifiedContact.CiiOrgId}/contacts/{eachModifiedContact.ContactPointId}";
      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.PartyId}");
        return JsonConvert.DeserializeObject<ContactOrgResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.PartyId}");
        return null;
      }
    }

    public async Task<List<ModifiedOrgContactInfo>> GetModifiedContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);
      List<ModifiedOrgContactInfo> ModifedOrgContacts = new List<ModifiedOrgContactInfo>();

      try
      {
        string url = $"contact-service/data/org-report?last-modified-date" + untilDateTime.ToString("dd-MM-yyyy HH:mm:ss");
        var client = _httpClientFactory.CreateClient("ContactWrapperApi");
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          ModifedOrgContacts = JsonConvert.DeserializeObject<List<ModifiedOrgContactInfo>>(content);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
      return ModifedOrgContacts;
    }

    public async Task<List<ModifiedUserContactInfo>> GetModifiedUserContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);
      List<ModifiedUserContactInfo> ModifedUserContacts = new List<ModifiedUserContactInfo>();

      try
      {
        string url = $"contact-service/data/user-report?last-modified-date" + untilDateTime.ToString("dd-MM-yyyy HH:mm:ss");
        var client = _httpClientFactory.CreateClient("ContactWrapperApi");
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          ModifedUserContacts = JsonConvert.DeserializeObject<List<ModifiedUserContactInfo>>(content);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
      return ModifedUserContacts;
    }

    private async Task<List<ModifiedSiteContactInfo>> GetModifiedSiteContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);
      List<ModifiedSiteContactInfo> ModifedSiteContacts = new List<ModifiedSiteContactInfo>();

      try
      {
        string url = $"contact-service/data/site-report?last-modified-date" + untilDateTime.ToString("dd-MM-yyyy HH:mm:ss");
        var client = _httpClientFactory.CreateClient("ContactWrapperApi");
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          ModifedSiteContacts = JsonConvert.DeserializeObject<List<ModifiedSiteContactInfo>>(content);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
      return ModifedSiteContacts;      
    }
  }
}
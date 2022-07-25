﻿using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
      
        var totalNumberOfItemsDuringThisSchedule = 0;

        ContactResponseInfo contactModuleList = new ContactResponseInfo();

        ////////////////// Organisation Contact Report - Start /////////////////////////////

        var listOfAllModifiedOrgContactId = await GetModifiedContactIds(); // ORG
        if (listOfAllModifiedOrgContactId == null || listOfAllModifiedOrgContactId.Count() == 0)
        {
          _logger.LogInformation("No Org Contacts are found");
          return;
        }

        _logger.LogInformation($"Total number of Org Contact => {listOfAllModifiedOrgContactId.Count()}");

        // spliting the jobs
        int sizeOrg = _appSettings.MaxNumbeOfRecordInAReport;
        _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
        var indexOrg = 0;

        List<ContactOrgResponseInfo> contactOrgResponseInfo = new List<ContactOrgResponseInfo>();       
        foreach (var eachModifiedOrgContact in listOfAllModifiedOrgContactId)
        {
          indexOrg++;
          _logger.LogInformation($"trying to get contact details of {indexOrg} nd contact");

         
            try
            {
              _logger.LogInformation("Calling wrapper API to get Contact Details");
              // Call the Org Contact Information
              var client = _httpClientFactory.CreateClient("WrapperApi");
              var contactOrgResult = await GetOrgContactDetails(eachModifiedOrgContact, client);
              if (contactOrgResult != null)
              {
                contactOrgResult.contactType = "organisation";
                contactOrgResponseInfo.Add(contactOrgResult);              
              }
            }
            catch (Exception ex)
            {
              _logger.LogError($" XXXXXXXXXXXX Failed to retrieve contact details from Wrapper Api. UserId ={eachModifiedOrgContact.Item2} and Message - {ex.Message} XXXXXXXXXXXX");
            }

            if (listOfAllModifiedOrgContactId.Count != indexOrg && contactOrgResponseInfo.Count < sizeOrg)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Contacts in this Batch => {contactOrgResponseInfo.Count()}");
            totalNumberOfItemsDuringThisSchedule += contactOrgResponseInfo.Count();

            _logger.LogInformation("After calling the wrapper API to get User Details");
          }

      contactModuleList.contactOrgResponseInfo = new List<ContactOrgResponseInfo>(contactOrgResponseInfo);

      ////////////////// Organisation Contact Report - End /////////////////////////////


      ////////////////// User Contact Report - Start /////////////////////////////

      var listOfAllModifiedUserContactId = await GetModifiedUserContactIds(); // User
        List<ContactUserResponseInfo> contactUserResponseInfo = new List<ContactUserResponseInfo>();
        ContactUserResponseInfo contactUser = new ContactUserResponseInfo();
        foreach (var eachModifiedUserContact in listOfAllModifiedUserContactId)
        {
          // Call the Org Contact Information
          var client = _httpClientFactory.CreateClient("WrapperApi");
          var contactUserResult = await GetUserContactDetails(eachModifiedUserContact, client);
          if (contactUserResult != null)
          {
            contactUserResult.contactType = "user";
            contactUserResponseInfo.Add(contactUserResult);
          }
        }
      contactModuleList.contactUserResponseInfo = new List<ContactUserResponseInfo>(contactUserResponseInfo);
      ////////////////// User Contact Report - End /////////////////////////////
      

      ////////////////// Site Contact Report - Start /////////////////////////////

      var listOfAllModifiedSiteContactId = await GetModifiedSiteContactIds(); // Site
      List<ContactSiteResponseInfo> contactSiteResponseInfo = new List<ContactSiteResponseInfo>();
      ContactSiteResponseInfo contactSite = new ContactSiteResponseInfo();
      foreach (var eachModifiedSiteContact in listOfAllModifiedSiteContactId)
      {
        // Call the Org Contact Information
        var client = _httpClientFactory.CreateClient("WrapperApi");
        var contactSiteResult = await GetSiteContactDetails(eachModifiedSiteContact, client);
        if (contactSiteResult != null)
        {
          contactSiteResult.contactType = "site";
          contactSiteResponseInfo.Add(contactSiteResult);
        }
      }
      contactModuleList.contactSiteResponseInfo = new List<ContactSiteResponseInfo>(contactSiteResponseInfo);
      ////////////////// Site Contact Report - End /////////////////////////////

      /// My Byte Array - Start
      var fileByteArrayOrg = _csvConverter.ConvertToCSV(contactModuleList.contactOrgResponseInfo, "contact-org");
      var fileByteArrayUser =  _csvConverter.ConvertToCSV(contactModuleList.contactUserResponseInfo, "contact-user");
      var fileByteArraySite = _csvConverter.ConvertToCSV(contactModuleList.contactSiteResponseInfo, "contact-site");
      byte[] fileByteArray = fileByteArrayOrg.Concat(fileByteArrayUser).Concat(fileByteArraySite).ToArray();
      /// My Byte Array - End
      
      using (MemoryStream memStream = new MemoryStream(fileByteArray))
      {
        File.WriteAllBytes("allcontactInone.csv", fileByteArray);
      }

      _logger.LogInformation("After converting the list of user object into CSV format and returned byte Array");

      AzureResponse result = await _fileUploadToCloud.FileUploadToAzureBlobAsync(fileByteArray, "Contacts");
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
 

    private async Task<List<Tuple<string, int, int>>> GetModifiedSiteContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {

        var contactDetailsResult = await (from cdt in _dataContext.ContactDetail
                                          join cpt in _dataContext.ContactPoint on cdt.Id equals cpt.ContactDetailId
                                          join st in _dataContext.SiteContact on cpt.Id equals st.ContactPointId
                                          join org in _dataContext.Organisation on cpt.PartyId equals org.PartyId
                                          where cdt.LastUpdatedOnUtc > untilDateTime && !cdt.IsDeleted

                                          select new Tuple<string, int, int >(
                                            org.CiiOrganisationId, cpt.Id, st.Id)
                                      ).ToListAsync();

        return contactDetailsResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }

    private async Task<ContactSiteResponseInfo?> GetSiteContactDetails(Tuple<string, int, int> eachModifiedContact, HttpClient client)
    {

      string url = $"organisations/{eachModifiedContact.Item1}/sites/{eachModifiedContact.Item2}/contacts/{eachModifiedContact.Item3}";      

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.Item2}");
        return JsonConvert.DeserializeObject<ContactSiteResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.Item2}");
        return null;
      }
    }

    private async Task<ContactUserResponseInfo?> GetUserContactDetails(Tuple<int, int, int, string> eachModifiedContact, HttpClient client)
    {
      
      string url = $"users/contacts/{eachModifiedContact.Item1}?user-id={eachModifiedContact.Item4}";
     
      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.Item2}");
        return JsonConvert.DeserializeObject<ContactUserResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.Item2}");
        return null;
      }
    }

    

    private async Task<ContactOrgResponseInfo?> GetOrgContactDetails(Tuple<int, int, int, string> eachModifiedContact, HttpClient client)
    {     
      string url = $"organisations/{eachModifiedContact.Item4}/contacts/{eachModifiedContact.Item1}";
      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.Item2}");
       return JsonConvert.DeserializeObject<ContactOrgResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.Item2}");
        return null;
      }
    }

    public async Task<List<Tuple<int, int, int, string>>> GetModifiedContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        /*var userIds = await _dataContext.User.Where(
                          usr => !usr.IsDeleted && usr.LastUpdatedOnUtc > untilDateTime)
                          .Select(u => new Tuple<int, string>(u.Id, u.UserName)).ToListAsync();
        return userIds;*/

        /* var contactID = await _dataContext.ContactDetail.Where(
                         usr => !usr.IsDeleted && usr.LastUpdatedOnUtc > untilDateTime && usr.CreatedUserId != 0)
                         .OrderByDescending(m => m.LastUpdatedOnUtc)
                         .Select(u => new Tuple<int>(u.Id)).ToListAsync();

         var contactPointID = await _dataContext.ContactPoint.Where(
                        usr => !usr.IsDeleted && usr.ContactDetailId == contactID[0].Item1 && usr.CreatedUserId != 0)
                        .OrderByDescending(m => m.LastUpdatedOnUtc)
                        .Select(u => new Tuple<int, int>(u.Id, u.PartyId)).ToListAsync();

         var personID = await _dataContext.Person.Where(
                        usr => !usr.IsDeleted && usr.PartyId == contactPointID[1].Item2 && usr.CreatedUserId != 0)
                        .OrderByDescending(m => m.LastUpdatedOnUtc)
                        .Select(u => new Tuple<int>(u.OrganisationId)).ToListAsync();

         var ciiOrgId = await _dataContext.Organisation.Where(
                        usr => !usr.IsDeleted && usr.Id == personID[1].Item1 && usr.CreatedUserId != 0)
                        .OrderByDescending(m => m.LastUpdatedOnUtc)
                        .Select(u => new Tuple<string,List<Tuple<int,int>>>(u.CiiOrganisationId, contactPointID)).ToListAsync();
        */

        //string orgId = "627961658397339904"; int copoint = 8512; // ciiOrgId[0].item1 , contactPointID[0].Item1


        /*
         var contactDetailsResult = await(from cdt in _dataContext.ContactDetail
                                    join cpt in _dataContext.ContactPoint on cdt.Id equals cpt.ContactDetailId
                                    join pr in _dataContext.Person on cpt.PartyId equals pr.PartyId
                                    join org in _dataContext.Organisation on pr.OrganisationId equals org.Id

                                    where cdt.LastUpdatedOnUtc > untilDateTime && !cdt.IsDeleted 

                                    select new Tuple<int, int, int, string>(
                                      cpt.Id, cpt.PartyId, cpt.ContactDetailId,"") //, org.CiiOrganisationId)
                                      ).ToListAsync();
        */

        var contactDetailsResult = await (from cdt in _dataContext.ContactDetail
                                          join cpt in _dataContext.ContactPoint on cdt.Id equals cpt.ContactDetailId                                          
                                          join org in _dataContext.Organisation on cpt.PartyId equals org.PartyId
                                          where cdt.LastUpdatedOnUtc > untilDateTime && !cdt.IsDeleted

                                          select new Tuple<int, int, int, string>(
                                            cpt.Id, cpt.PartyId, cpt.ContactDetailId, org.CiiOrganisationId)
                                      ).ToListAsync();

        return contactDetailsResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }


    }

    public async Task<List<Tuple<int, int, int, string>>> GetModifiedUserContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {        

        var contactDetailsResult = await (from cdt in _dataContext.ContactDetail
                                          join cpt in _dataContext.ContactPoint on cdt.Id equals cpt.ContactDetailId
                                          join org in _dataContext.User on cpt.PartyId equals org.PartyId
                                          where cdt.LastUpdatedOnUtc > untilDateTime && !cdt.IsDeleted

                                          select new Tuple<int, int, int, string>(
                                            cpt.Id, cpt.PartyId, cpt.ContactDetailId, org.UserName)
                                      ).ToListAsync();

        return contactDetailsResult;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }


    }
  }
}
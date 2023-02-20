using CcsSso.Core.ReportingScheduler.Models;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql.Internal;
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
        orgContacts.ForEach(x =>  listOfAllModifiedContact.Add(new ContactModel { ContactType = "ORG", DetectedContact = x }));

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
              var client = _httpClientFactory.CreateClient("WrapperApi");

              switch (eachModifiedOrg.ContactType)
              {
                case "ORG":
                  var contactDetails = await GetOrgContactDetails((Tuple<int, int, int, string>)eachModifiedOrg.DetectedContact, client);
                  if (contactDetails != null)
                  {
                    contactDetails.contactType = "organisation";
                    contactDetailList.Add(new ContactDetailModel { ContactType = eachModifiedOrg.ContactType, ContactDetail = contactDetails });
                  }
                  break;
                case "USER":
                  var contactUserDetails = await GetUserContactDetails((Tuple<int, int, int, string>)eachModifiedOrg.DetectedContact, client);
                  if (contactUserDetails != null)
                  {
                    contactUserDetails.contactType = "user";
                    contactDetailList.Add(new ContactDetailModel { ContactType = eachModifiedOrg.ContactType, ContactDetail = contactUserDetails });
                  }
                  break;
                case "SITE":
                  var contactSiteDetails = await GetSiteContactDetails((Tuple<string, int, int>)eachModifiedOrg.DetectedContact, client);
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

           
            var fileByteArrayOrg = _csvConverter.ConvertToCSV(contactDetailList.Where(x=>x.ContactType=="ORG").Select(x=>(ContactOrgResponseInfo)x.ContactDetail).ToList(), "contact-org");
            var fileByteArrayUser = _csvConverter.ConvertToCSV(contactDetailList.Where(x => x.ContactType == "USER").Select(x =>(ContactUserResponseInfo) x.ContactDetail).ToList(), "contact-user");
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

      string url = $"users/contacts/{eachModifiedContact.Item1}?user-id={HttpUtility.UrlEncode(eachModifiedContact.Item4)}";

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
        var contactDetailsResult = await (from p in _dataContext.Person
                                          join prt in _dataContext.Party on p.PartyId equals prt.Id
                                          join cp in _dataContext.ContactPoint on prt.Id equals cp.PartyId into gcp
                                          from subgcp in gcp.Where(x => x.PartyTypeId == 4).DefaultIfEmpty()
                                          join cpuser in _dataContext.ContactPoint on new { subgcp.ContactDetailId, PartyType = 2 } equals new { cpuser.ContactDetailId, PartyType = cpuser.PartyTypeId }
                                          join org in _dataContext.Organisation on p.OrganisationId equals org.Id
                                          join vrad in _dataContext.VirtualAddress on subgcp.ContactDetailId equals vrad.ContactDetailId into gvrad
                                          from subgvrad in gvrad.DefaultIfEmpty()
                                          where ((p.LastUpdatedOnUtc > untilDateTime || subgcp.LastUpdatedOnUtc > untilDateTime || cpuser.LastUpdatedOnUtc > untilDateTime || subgvrad.LastUpdatedOnUtc > untilDateTime) && cpuser.IsDeleted == false)
                                          select new Tuple<int, int, int, string>(
                                       cpuser.Id, cpuser.PartyId, subgcp.ContactDetailId, org.CiiOrganisationId)
                             ).Distinct().ToListAsync();
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

        var contactDetailsResult = await (from p in _dataContext.Person
                                          join prt in _dataContext.Party on p.PartyId equals prt.Id
                                          join cp in _dataContext.ContactPoint on prt.Id equals cp.PartyId into gcp
                                          from subgcp in gcp.Where(x => x.PartyTypeId == 4).DefaultIfEmpty()
                                          join cpuser in _dataContext.ContactPoint on subgcp.ContactDetailId equals cpuser.ContactDetailId into gcpuser
                                          from subgcpuser in gcpuser.Where(x => x.PartyTypeId == 3).DefaultIfEmpty()
                                          join u in _dataContext.User on subgcpuser.PartyId equals u.PartyId
                                          join vrad in _dataContext.VirtualAddress on subgcp.ContactDetailId equals vrad.ContactDetailId into gvrad
                                          from subgvrad in gvrad.DefaultIfEmpty()
                                          where ((p.LastUpdatedOnUtc > untilDateTime || subgcp.LastUpdatedOnUtc > untilDateTime || subgvrad.LastUpdatedOnUtc > untilDateTime) && subgcp.IsDeleted == false)
                                          select new Tuple<int, int, int, string>(
                                       subgcpuser.Id, subgcpuser.PartyId, subgcpuser.ContactDetailId, u.UserName)
                                      ).Distinct().ToListAsync();

        return contactDetailsResult;

      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }

    }

    private async Task<List<Tuple<string, int, int>>> GetModifiedSiteContactIds()
    {
      var dataDuration = _appSettings.ReportDataDurations.ContactReportingDurationInMinutes;
      var untilDateTime = _dataTimeService.GetUTCNow().AddMinutes(-dataDuration);

      try
      {
        var detectedContacts = new List<Tuple<string, int, int>>();

        var directContact = await (from p in _dataContext.Person
                                   join prt in _dataContext.Party on p.PartyId equals prt.Id
                                   join cp in _dataContext.ContactPoint
                                         on new { PartyType = 4, PartyId = prt.Id } equals new { PartyType = cp.PartyTypeId, PartyId = cp.PartyId }

                                   join cplink in _dataContext.ContactPoint
                                     on cp.ContactDetailId equals cplink.ContactDetailId into gcplink
                                   from subcplink in gcplink.DefaultIfEmpty()


                                   join sc in _dataContext.SiteContact on new { Id = subcplink.Id, IsDeleted = false } equals new { Id = sc.ContactId, IsDeleted = sc.IsDeleted }

                                   join cpsite in _dataContext.ContactPoint
                                         on new { ContactPointId = sc.ContactPointId, isSite = true } equals new { ContactPointId = cpsite.Id, isSite = cpsite.IsSite }

                                   join vrad in _dataContext.VirtualAddress
                                         on cp.ContactDetailId equals vrad.ContactDetailId into gvrad
                                   from subgvrad in gvrad.DefaultIfEmpty()

                                   join org in _dataContext.Organisation
                                        on p.OrganisationId equals org.Id into gorg
                                   from subgorg in gorg.DefaultIfEmpty()

                                   where (p.LastUpdatedOnUtc > untilDateTime || cp.LastUpdatedOnUtc > untilDateTime || subgvrad.LastUpdatedOnUtc > untilDateTime || sc.LastUpdatedOnUtc > untilDateTime)

                                   select new Tuple<string, int, int>(subgorg.CiiOrganisationId, sc.ContactPointId, sc.Id)).Distinct().ToListAsync();


        var assignedContact = await (from sc in _dataContext.SiteContact
                                     join cp in _dataContext.ContactPoint
                                           on sc.ContactId equals cp.Id
                                     join pty in _dataContext.Party on cp.PartyId equals pty.Id
                                     join p in _dataContext.Person on pty.Id equals p.PartyId
                                     join org in _dataContext.Organisation on p.OrganisationId equals org.Id
                                     where sc.LastUpdatedOnUtc > untilDateTime && sc.IsDeleted == false && sc.AssignedContactType != 0
                                     select new Tuple<string, int, int>(org.CiiOrganisationId, sc.ContactPointId, sc.Id)).Distinct().ToListAsync();

        detectedContacts.AddRange(directContact);
        detectedContacts.AddRange(assignedContact);

        return detectedContacts.Distinct().ToList();

      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error");
        throw;
      }
    }


  }
}
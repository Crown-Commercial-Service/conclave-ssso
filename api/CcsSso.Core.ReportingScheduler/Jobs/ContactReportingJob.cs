using CcsSso.Core.ReportingScheduler.Models;
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

        var listOfAllModifiedContactId = await GetModifiedContactIds(); // ORG

        //var listOfAllModifiedUserContactId = await GetModifiedUserContactIds(); // USER



        if (listOfAllModifiedContactId == null || listOfAllModifiedContactId.Count() == 0)
        {
          _logger.LogInformation("No Contacts are found");
          return;
        }

        _logger.LogInformation($"Total number of Contact => {listOfAllModifiedContactId.Count()}");

        // spliting the jobs
        int size = _appSettings.MaxNumbeOfRecordInAReport;
        _logger.LogInformation($"Max number of record in a report from configuartion settings => {_appSettings.MaxNumbeOfRecordInAReport}");
        var index = 0;

        List<ContactResponseInfo> contactDetailList = new List<ContactResponseInfo>();

        foreach (var eachModifiedContact in listOfAllModifiedContactId)
        {
          index++;
          _logger.LogInformation($"trying to get contact details of {index} nd contact");

          try
          {
            try
            {
              _logger.LogInformation("Calling wrapper API to get Contact Details");

              // Call the Organisation - Contact Information
              var client = _httpClientFactory.CreateClient("WrapperApi");
              var orgContactInformation = await GetOrgContactDetails(eachModifiedContact, client);
              if (orgContactInformation != null)
              {
                orgContactInformation.contactDeducted = "organisation";                
                contactDetailList.Add(orgContactInformation);
              }

            

              // Call the Site - Contact Information



            }
            catch (Exception ex)
            {
              _logger.LogError($" XXXXXXXXXXXX Failed to retrieve contact details from Wrapper Api. UserId ={eachModifiedContact.Item2} and Message - {ex.Message} XXXXXXXXXXXX");
            }

            if (listOfAllModifiedContactId.Count != index && contactDetailList.Count < size)
            {
              continue;
            }

            _logger.LogInformation($"Total number of Contacts in this Batch => {contactDetailList.Count()}");
            totalNumberOfItemsDuringThisSchedule += contactDetailList.Count();

            _logger.LogInformation("After calling the wrapper API to get User Details");

            var fileByteArray = _csvConverter.ConvertToCSV(contactDetailList, "contact");

            using (MemoryStream memStream = new MemoryStream(fileByteArray))
            {
              File.WriteAllBytes("contactorg.csv", fileByteArray);
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

            _logger.LogError($"XXXXXXXXXXXX Failed to transfer the report. Number of users in this set {contactDetailList.Count()} XXXXXXXXXXXX");
            _logger.LogError("");

          }
          contactDetailList.Clear();
          await Task.Delay(5000);

        }
        _logger.LogInformation($"Total number of users exported during this schedule => {totalNumberOfItemsDuringThisSchedule}");
      }
      catch (Exception ex)
      {

        _logger.LogError($"XXXXXXXXXXXX Failed to transfer. Outer exception - {ex.Message} XXXXXXXXXXXX");
        _logger.LogError("");
      }
    }

    private async Task<ContactResponseInfo?> GetUserContactDetails(Tuple<int, int, int, string> eachModifiedContact, HttpClient client)
    {
      //string orgId = "627961658397339904"; int copoint = 8512;
       
      string url = $"users/?user-id={eachModifiedContact.Item4}/contacts/{eachModifiedContact.Item1}";
      //string url = $"organisations/{orgId}/contacts/{copoint}";

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.Item2}");
        return JsonConvert.DeserializeObject<ContactResponseInfo>(content);

      }
      else
      {
        _logger.LogError($"No Users retrived for contact ID-{eachModifiedContact.Item2}");
        return null;
      }
    }

    private async Task<ContactResponseInfo?> GetOrgContactDetails(Tuple<int, int, int, string> eachModifiedContact, HttpClient client)
    {
      //string orgId = "627961658397339904"; int copoint = 8512;
      string url = $"organisations/{eachModifiedContact.Item4}/contacts/{eachModifiedContact.Item1}";
      //string url = $"organisations/{orgId}/contacts/{copoint}";

      var response = await client.GetAsync(url);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Retrived contact details for contact ID-{eachModifiedContact.Item2}");
       return JsonConvert.DeserializeObject<ContactResponseInfo>(content);

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
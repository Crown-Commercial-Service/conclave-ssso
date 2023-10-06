using Amazon.S3;
using Amazon.SimpleSystemsManagement.Model;
using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.DataMigrationJobScheduler.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Model;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Core.Domain.Dtos.Wrapper;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3.Model;
using S3ConfigurationInfo = CcsSso.Core.DataMigrationJobScheduler.Model.S3ConfigurationInfo;
using IAwsS3Service = CcsSso.Core.DataMigrationJobScheduler.Contracts.IAwsS3Service;
using CcsSso.Dtos.Domain.Models;
using Newtonsoft.Json;
using System.Net;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CcsSso.Domain.Contracts;
using CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts;
using IWrapperOrganisationService = CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts.IWrapperOrganisationService;
using CcsSso.Domain.Dtos;
using System.Xml.Linq;

namespace CcsSso.Core.DataMigrationJobScheduler.Services
{
  public class FileUploadJobService : IFileUploadJobService
  {
    private readonly IDataContext _dataContext;
    private readonly ILogger<IFileUploadJobService> _logger;
    private readonly DataMigrationAppSettings _appSettings;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly S3ConfigurationInfo _s3ConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAwsS3Service _awsS3Service;

    public FileUploadJobService(IServiceScopeFactory factory,
      DataMigrationAppSettings appSettings,
      ILogger<FileUploadJobService> logger,
       IWrapperOrganisationService wrapperOrganisationService, Model.S3ConfigurationInfo s3ConfigurationInfo, IHttpClientFactory httpClientFactory,IAwsS3Service awsS3Service)
    {
      _dataContext = factory.CreateScope().ServiceProvider.GetRequiredService<IDataContext>();
      _appSettings = appSettings;
      _wrapperOrganisationService = wrapperOrganisationService;
      _logger = logger;
      _s3ConfigurationInfo = s3ConfigurationInfo;
      _httpClientFactory = httpClientFactory;
      _awsS3Service = awsS3Service;
    }

    public async Task PerformFileUploadJobAsync()
    {
      var files =await _wrapperOrganisationService.GetDataMigrationFilesList();
      if (files.DataMigrationList.Any())
      {
        List<Task> taskList = new List<Task>();
        foreach (var file in files.DataMigrationList)
        {
          var fileContentString = await _awsS3Service.ReadObjectDataStringAsync(file.FileKey, _s3ConfigurationInfo.DataMigrationBucketName);
          var fileObject = await _awsS3Service.ReadObjectData(file.FileKey, _s3ConfigurationInfo.DataMigrationBucketName);

          var client = _httpClientFactory.CreateClient("CiiApi");
          using (var formData = new MultipartFormDataContent())
          {
            using (var s3DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContentString)))
            {
              // Create a StreamContent from the S3 data stream
              var streamContent = new StreamContent(s3DataStream);

              // Add the StreamContent to the form data
              formData.Add(streamContent, "file", "data.csv"); // You can specify the file name as needed


              // Make a POST request to the CII API
              var response = await client.PostAsync($"data-migration/migrate/format/csv", formData);
              if (response.IsSuccessStatusCode)
              {
                var fileKey = GetUploadFileKey(file.FileKey, true);
                await _awsS3Service.WritingAFileDataAsync(fileKey, "text/csv", _s3ConfigurationInfo.DataMigrationBucketName, fileContentString);
                taskList.Add(_wrapperOrganisationService.UpdateDataMigrationFileStatus(new DataMigrationStatusRequest { Id = file.Id, DataMigrationStatus = DataMigrationStatus.Completed }));
              }
              else
              {
                var fileKey = GetUploadFileKey(file.FileKey, false);
                await _awsS3Service.WritingAFileDataAsync(fileKey, "text/csv", _s3ConfigurationInfo.DataMigrationBucketName, fileContentString);
                taskList.Add(_wrapperOrganisationService.UpdateDataMigrationFileStatus(new DataMigrationStatusRequest { Id = file.Id, DataMigrationStatus = DataMigrationStatus.Failed }));
              }
            }

            if (taskList.Count > 0)
            {
              await Task.WhenAll(taskList);
            }
          }
        }
      }
    }

    /// <summary>
    /// Get the full file key in "folderName/organisationId/fileKeyId" format.
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="fileKeyId"></param>
    /// <returns></returns>
    private string GetUploadFileKey(string fileKeyId,bool isSuccess)
    {
      string fileKeyinfo= fileKeyId.Substring(fileKeyId.IndexOf('/') + 1);
      if (isSuccess)
      {
        return $"{_s3ConfigurationInfo.DataMigrationSuccessFolderName}/{fileKeyinfo}";
      }
      else
      {
        return $"{_s3ConfigurationInfo.DataMigrationFailedFolderName}/{fileKeyinfo}";
      }
    }

  }
}

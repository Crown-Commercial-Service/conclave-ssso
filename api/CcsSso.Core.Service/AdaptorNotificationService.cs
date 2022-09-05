using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class AdaptorNotificationService : IAdaptorNotificationService
  {
    private readonly IDataContext _dataContext;
    private readonly IAwsSqsService _awsSqsService;
    private readonly ApplicationConfigurationInfo _appConfig;
    private readonly ILocalCacheService _localCacheService;
    public AdaptorNotificationService(IDataContext dataContext, IAwsSqsService awsSqsService, ApplicationConfigurationInfo appConfig,
      ILocalCacheService localCacheService)
    {
      _dataContext = dataContext;
      _awsSqsService = awsSqsService;
      _appConfig = appConfig;
      _localCacheService = localCacheService;
    }

    /// <summary>
    /// Notify individual contacts
    /// Since there can be individual contacts without any relationship to any other entity (Org,Site,User) were aksed to push the message to all the services.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task NotifyContactChangeAsync(string operation, int contactId)
    {
      if (_appConfig.EnableAdapterNotifications)
      {
        try
        {
          var serviceClientIds = _localCacheService.GetValue<List<string>>("SERVICE_CLIENT_IDS");
          if (serviceClientIds == null || !serviceClientIds.Any())
          {
            serviceClientIds = await _dataContext.CcsService.Where(sc => sc.ServiceClientId != _appConfig.DashboardServiceClientId)
              .Select(s => s.ServiceClientId).ToListAsync();
            _localCacheService.SetValue<List<string>>("SERVICE_CLIENT_IDS", serviceClientIds, new TimeSpan(0, _appConfig.InMemoryCacheExpirationInMinutes, 0));
          }

          List<SqsMessageDto> sqsMessageDtos = new();
          foreach (var serviceClientId in serviceClientIds)
          {
            SqsMessageDto sqsMessageDto = new()
            {
              MessageBody = serviceClientId,
              StringCustomAttributes = new Dictionary<string, string>
              {
                { QueueConstant.OperationEntity, ConclaveEntityNames.Contact },
                { QueueConstant.OperationName, operation }
              },
              NumberCustomAttributes = new Dictionary<string, int>
              {
                { QueueConstant.ContactIdAttribute, contactId }
              }
            };
            sqsMessageDtos.Add(sqsMessageDto);
          }
          await _awsSqsService.SendMessageBatchAsync(_appConfig.QueueUrlInfo.AdaptorNotificationQueueUrl, $"Contact-{contactId}", sqsMessageDtos);
        }

        catch (Exception ex)
        {
          Console.WriteLine($"Error notifying adapter for contact change. ContactId: {contactId}, Error: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Notify contact point changes to relevant services
    /// </summary>
    /// <param name="contactEntity"></param>
    /// <param name="operation"></param>
    /// <param name="organisationId"></param>
    /// <param name="contactIds"></param>
    /// <returns></returns>
    public async Task NotifyContactPointChangesAsync(string contactEntity, string operation, string organisationId, List<int> contactIds)
    {
      if (_appConfig.EnableAdapterNotifications && contactIds.Any())
      {
        try
        {
          var serviceClientIds = GetServiceClientIds(organisationId);
          List<SqsMessageDto> sqsMessageDtos = new();
          foreach (var serviceClientId in serviceClientIds)
          {
            foreach (var contactId in contactIds)
            {
              SqsMessageDto sqsMessageDto = new()
              {
                MessageBody = serviceClientId,
                StringCustomAttributes = new Dictionary<string, string>
              {
                { QueueConstant.OperationEntity, contactEntity },
                { QueueConstant.OperationName, operation }
              },
                NumberCustomAttributes = new Dictionary<string, int>
              {
                { QueueConstant.ContactIdAttribute, contactId }
              }
              };
              sqsMessageDtos.Add(sqsMessageDto);
            }
          }
          await _awsSqsService.SendMessageBatchAsync(_appConfig.QueueUrlInfo.AdaptorNotificationQueueUrl, $"Contactpoint-{organisationId}", sqsMessageDtos);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error notifying adapter for contact point change. ContactIds: {string.Join(",", contactIds)}, Error {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Notify organisation changes to relevant services
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="organisationId"></param>
    /// <returns></returns>
    public async Task NotifyOrganisationChangeAsync(string operation, string organisationId)
    {
      if (_appConfig.EnableAdapterNotifications)
      {
        try
        {
          var serviceClientIds = GetServiceClientIds(organisationId);
          List<SqsMessageDto> sqsMessageDtos = new();
          foreach (var serviceClientId in serviceClientIds)
          {
            SqsMessageDto sqsMessageDto = new()
            {
              MessageBody = serviceClientId,
              StringCustomAttributes = new Dictionary<string, string>
            {
              { QueueConstant.OperationEntity, ConclaveEntityNames.OrgProfile },
              { QueueConstant.OperationName, operation },
              { QueueConstant.OrganisationIdAttribute, organisationId },
            }
            };
            sqsMessageDtos.Add(sqsMessageDto);
          }
          await _awsSqsService.SendMessageBatchAsync(_appConfig.QueueUrlInfo.AdaptorNotificationQueueUrl, organisationId, sqsMessageDtos);
        }

        catch (Exception ex)
        {
          Console.WriteLine($"Error notifying adapter for organisation change. OrganisationId: {organisationId}, Error {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Notify user changes to relevant services
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="userName"></param>
    /// <param name="organisationId"></param>
    /// <returns></returns>
    public async Task NotifyUserChangeAsync(string operation, string userName, string organisationId)
    {
      if (_appConfig.EnableAdapterNotifications)
      {
        try
        {
          var serviceClientIds = GetServiceClientIds(organisationId);
          List<SqsMessageDto> sqsMessageDtos = new();
          foreach (var serviceClientId in serviceClientIds)
          {
            SqsMessageDto sqsMessageDto = new()
            {
              MessageBody = serviceClientId,
              StringCustomAttributes = new Dictionary<string, string>
              {
                { QueueConstant.OperationEntity, ConclaveEntityNames.UserProfile },
                { QueueConstant.OperationName, operation },
                { QueueConstant.UserNameAttribute, userName },
              }
            };
            sqsMessageDtos.Add(sqsMessageDto);
          }
          await _awsSqsService.SendMessageBatchAsync(_appConfig.QueueUrlInfo.AdaptorNotificationQueueUrl, userName, sqsMessageDtos);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error notifying adapter for user change. UserName: {userName}, Error {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Get the service client ids of the organisation
    /// </summary>
    /// <param name="organisationId"></param>
    /// <returns></returns>
    private List<string> GetServiceClientIds(string organisationId)
    {

      var serviceClientIds = _localCacheService.GetValue<List<string>>($"ORGANISATION_SERVICE_CLIENT_IDS-{organisationId}");

      if (serviceClientIds == null || !serviceClientIds.Any())
      {
        var organisation = _dataContext.Organisation
        .Include(o => o.OrganisationEligibleRoles).ThenInclude(oer => oer.CcsAccessRole)
        .ThenInclude(ar => ar.ServiceRolePermissions).ThenInclude(srp => srp.ServicePermission).ThenInclude(sp => sp.CcsService)
        .FirstOrDefault(o => o.CiiOrganisationId == organisationId);

        var serviceClients = organisation.OrganisationEligibleRoles
          .Where(oer => !oer.IsDeleted && oer.CcsAccessRole.ServiceRolePermissions.Any()) // TODO Check whther therw are any null values in the list whithout this
          .Select(oer => oer.CcsAccessRole.ServiceRolePermissions.FirstOrDefault()?.ServicePermission.CcsService).ToList();

        serviceClientIds = serviceClients.Where(sc => sc.ServiceClientId != _appConfig.DashboardServiceClientId && sc.ServiceClientId != null)
          .Select(sc => sc.ServiceClientId).Distinct().ToList();

        _localCacheService.SetValue<List<string>>($"ORGANISATION_SERVICE_CLIENT_IDS-{organisationId}", serviceClientIds, new TimeSpan(0, _appConfig.InMemoryCacheExpirationInMinutes, 0));
      }      

      return serviceClientIds ?? new List<string>();
    }
  }
}

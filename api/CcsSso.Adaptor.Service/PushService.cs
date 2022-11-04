using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Domain.Excecptions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class PushService : IPushService
  {
    private readonly AdaptorRequestContext _requestContext;
    private AppSetting _appSetting;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAwsSqsService _awsSqsService;
    private readonly IConsumerService _consumerService;
    private readonly IUserService _userService; 
    private readonly IOrganisationService _organisationService;
    private readonly IContactService _contactService;

    public PushService(AdaptorRequestContext requestContext, AppSetting appSetting, IHttpClientFactory httpClientFactory,
      IAwsSqsService awsSqsService, IConsumerService consumerService, IUserService userService, IOrganisationService organisationService,
      IContactService contactService)
    {
      _requestContext = requestContext;
      _appSetting = appSetting;
      _httpClientFactory = httpClientFactory;
      _awsSqsService = awsSqsService;
      _consumerService = consumerService;
      _userService = userService;
      _organisationService = organisationService;
      _contactService = contactService;
    }

    /// <summary>
    /// Handle the push message details recieved from sqs listner
    /// </summary>
    /// <param name="sqsMessageResponseDto"></param>
    /// <returns></returns>
    public async Task PublishPushDataAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      var entityName = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.OperationEntity).Value;

      switch (entityName) {
        case ConclaveEntityNames.UserProfile:
          {
            var userName  = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.UserNameAttribute).Value;
            var operation  = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.OperationName).Value;
            Dictionary<string, object> result;
            if (operation != OperationType.Delete)
            {
              result = await _userService.GetUserAsync(userName);
              // Status can be used for local tests to check the message inside SQS (To check the operation happen to entity). Only for local development. Comment out for actual deployments
              //result.Add("Status", operation == OperationType.Create ? "Created": "Updated");
            }
            else
            {
              result = new Dictionary<string, object>
              {
                { QueueConstant.UserNameAttribute, userName },
                //{ "Status", "Deleted" }
              };
            }
            await NotifyPushDataToQueueAsync(result, ConclaveEntityNames.UserProfile);

            break;
          }
        case ConclaveEntityNames.OrgProfile:
          {
            var orgId = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.OrganisationIdAttribute).Value;
            var operation = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.OperationName).Value;
            Dictionary<string, object> result;
            if (operation != OperationType.Delete)
            {
              result = await _organisationService.GetOrganisationAsync(orgId);
              //result.Add("Status", operation == OperationType.Create ? "Created" : "Updated");
            }
            else
            {
              result = new Dictionary<string, object>
              {
                { QueueConstant.OrganisationIdAttribute, orgId },
                //{ "Status", "Deleted" }
              };
            }
            await NotifyPushDataToQueueAsync(result, ConclaveEntityNames.OrgProfile);
            break;
          }
        case ConclaveEntityNames.Contact:
        case ConclaveEntityNames.UserContact:
        case ConclaveEntityNames.OrgContact:
        case ConclaveEntityNames.SiteContact:
          {
            var contactId = sqsMessageResponseDto.NumberCustomAttributes.First(a => a.Key == QueueConstant.ContactIdAttribute).Value;
            var operation = sqsMessageResponseDto.StringCustomAttributes.First(a => a.Key == QueueConstant.OperationName).Value;
            Dictionary<string, object> result;
            if (operation != OperationType.Delete)
            {
              result = await _contactService.GetContactAsync(contactId);
              //result.Add("Status", operation == OperationType.Create ? "Created" : "Updated");
            }
            else
            {
              result = new Dictionary<string, object>
              {
                { QueueConstant.ContactIdAttribute, contactId },
                //{ "Status", "Deleted" }
              };
            }
            await NotifyPushDataToQueueAsync(result, ConclaveEntityNames.Contact);
            break;
          }
        default:
          {
            break;
          }
      }
    }

    /// <summary>
    /// Send the push data to a queue
    /// </summary>
    /// <param name="pushData"></param>
    /// <param name="conclaveEntityName"></param>
    /// <returns></returns>
    private async Task NotifyPushDataToQueueAsync(Dictionary<string, object> pushData, string conclaveEntityName)
    {
      var pushSubscriptionData = await _consumerService.GetSubscriptionDetail(_requestContext.ConsumerId, conclaveEntityName);   
      if (pushSubscriptionData == null)
      {
        throw new CcsSsoException(ErrorConstant.NoSubscriptionFound);
      }

      SqsMessageDto sqsMessageDto = new SqsMessageDto
      {
        MessageBody = JsonConvert.SerializeObject(pushData),
        StringCustomAttributes = new Dictionary<string, string>
        {
          { "URL", pushSubscriptionData.SubscriptionUrl },
          { "FORMAT", pushSubscriptionData.FomatFileType },
          { "APIKEY", pushSubscriptionData.SubscriptionApiKey },
        }
      };

      await _awsSqsService.SendMessageAsync(_appSetting.QueueUrlInfo.PushDataQueueUrl, pushSubscriptionData.ConsumerClientId, sqsMessageDto);
    }
  }
}

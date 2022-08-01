using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public partial class OrganisationProfileService: IOrganisationProfileService
  {
    public async Task UpdateIdentityProviderAsync(OrgIdentityProviderSummary orgIdentityProviderSummary)
    {
      if (orgIdentityProviderSummary.ChangedOrgIdentityProviders != null && orgIdentityProviderSummary.ChangedOrgIdentityProviders.Any()
        && !string.IsNullOrEmpty(orgIdentityProviderSummary.CiiOrganisationId))
      {
        var organisation = await _dataContext.Organisation
                                .Where(o => !o.IsDeleted && o.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
                                .FirstOrDefaultAsync();

        if (organisation != null)
        {
          // This will include all idps including "none"
          var identityProviderList = await _dataContext.IdentityProvider.Where(idp => !idp.IsDeleted).ToListAsync();

          var organisationIdps = await _dataContext.OrganisationEligibleIdentityProvider
            .Include(o => o.IdentityProvider)
            .Where(oeidp => !oeidp.IsDeleted && oeidp.Organisation.CiiOrganisationId == orgIdentityProviderSummary.CiiOrganisationId)
            .ToListAsync();


          var userNamePasswordIdentityProvider = organisationIdps.FirstOrDefault(ip => ip.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);

          var idpList = orgIdentityProviderSummary.ChangedOrgIdentityProviders.Where(ip => identityProviderList.Select(tl => tl.Id).Contains(ip.Id));

          //delete the idp provider from the list
          var idpRemovedList = idpList.Where(idp => !idp.Enabled).Select(r => r.Id).ToList();

          //if (idpRemovedList.Contains(userNamePasswordIdentityProvider.Id))
          //{
          //  throw new CcsSsoException("ERROR_USERNAME_PASSWORD_IDP_REQUIRED");
          //}

          var orgIdpsToDelete = organisationIdps.Where(oidp => idpRemovedList.Contains(oidp.IdentityProvider.Id)).ToList();

          orgIdpsToDelete.ForEach((d) =>
          {
            d.IsDeleted = true;
          });

          List<User> users = await GetAffectedUsersByRemovedIdp(orgIdentityProviderSummary.CiiOrganisationId, idpRemovedList);
          List<Task> asyncTaskList = GetTaskListToRemoveUsers(organisation, userNamePasswordIdentityProvider, idpRemovedList, users, identityProviderList);

          AddIdpsToOrganisation(organisation, idpList);

          await _dataContext.SaveChangesAsync();


          await AddIdpsToUser(organisation, idpList);

          await _dataContext.SaveChangesAsync();


          await Task.WhenAll(asyncTaskList);
          //Invalidate redis
          var invalidatingCacheKeyList = users.Select(u => $"{CacheKeyConstant.User}-{u.UserName}").ToArray();
          await _wrapperCacheService.RemoveCacheAsync(invalidatingCacheKeyList);

          // Notify the adapter
          await _adapterNotificationService.NotifyOrganisationChangeAsync(OperationType.Update, organisation.CiiOrganisationId);
        }
        else
        {
          throw new ResourceNotFoundException();
        }
      }
    }

    private async Task AddIdpsToUser(Organisation organisation, IEnumerable<OrgIdentityProvider> idpList)
    {
      var idpAddedList = idpList.Where(idp => idp.Enabled).ToList();

      // Add new idps
      var newIdpIds = idpAddedList.Select(x => x.Id).ToList();
      //List<User> users = await GetAffectedUsersByRemovedIdp(organisation.CiiOrganisationId, newIdpIds);
      var organisationIdps = await _dataContext.OrganisationEligibleIdentityProvider
         .Include(o => o.IdentityProvider)
         .Where(oeidp => !oeidp.IsDeleted && oeidp.Organisation.CiiOrganisationId == organisation.CiiOrganisationId
         && newIdpIds.Contains(oeidp.IdentityProvider.Id)).ToListAsync();

      var organisationUser = await GetOrganisationUser(organisation.CiiOrganisationId);

      if (organisationIdps.Count == 0)
      {
        return;
      }

      foreach (var user in organisationUser)
      {
        List<UserIdentityProvider> newUserIdentityProviderList = new();

        organisationIdps.ForEach((eachOrgIdps) =>
        {
          if (!user.UserIdentityProviders.Any(uidp => !uidp.IsDeleted && uidp.OrganisationEligibleIdentityProviderId == eachOrgIdps.Id))
          {
            newUserIdentityProviderList.Add(new UserIdentityProvider { UserId = user.Id, OrganisationEligibleIdentityProviderId = eachOrgIdps.Id });
          }
        });
        if (newUserIdentityProviderList.Count > 0)
        {
          user.UserIdentityProviders.AddRange(newUserIdentityProviderList);
        }
      }
    }


    private void AddIdpsToOrganisation(Organisation organisation, IEnumerable<OrgIdentityProvider> idpList)
    {
      var idpAddedList = idpList.Where(idp => idp.Enabled).ToList();

      foreach (var idp in idpAddedList)
      {
        var addedOrganisationEligibleIdentityProvider = new OrganisationEligibleIdentityProvider
        {
          OrganisationId = organisation.Id,
          IdentityProviderId = idp.Id
        };
        _dataContext.OrganisationEligibleIdentityProvider.Add(addedOrganisationEligibleIdentityProvider);
      }
    }

    private List<Task> GetTaskListToRemoveUsers(Organisation organisation, OrganisationEligibleIdentityProvider userNamePasswordIdentityProvider, List<int> idpRemovedList, List<User> users, List<IdentityProvider> identityProviders)
    {
      var asyncTaskList = new List<Task>();
      var securityApiCallTaskList = new List<Task>();
      users.ForEach(async (user) =>
      {
        asyncTaskList.Add(_adapterNotificationService.NotifyUserChangeAsync(OperationType.Update, user.UserName, organisation.CiiOrganisationId));


        // Delete before add the record
        var recordsToDelete = user.UserIdentityProviders.Where(uip => !uip.IsDeleted && idpRemovedList.Select(i => i).Contains(uip.OrganisationEligibleIdentityProvider.IdentityProviderId)).ToList();
        foreach (var uidp in recordsToDelete)
        {
          uidp.IsDeleted = true;
        }

        var removedIdpsName = user.UserIdentityProviders.Select(uip => !uip.IsDeleted && idpRemovedList.Select(i => i).Contains(uip.OrganisationEligibleIdentityProvider.IdentityProviderId)).ToList();



        var availableIdps = user.UserIdentityProviders.Where(uidp => !uidp.IsDeleted).Select(uip => uip.OrganisationEligibleIdentityProvider.IdentityProviderId).Except(idpRemovedList.Select(i => i));

        if (!availableIdps.Any())
        {
          SecurityApiUserInfo securityApiUserInfo = new SecurityApiUserInfo
          {
            Email = user.UserName,
            FirstName = user.Party.Person.FirstName,
            LastName = user.Party.Person.LastName,
            UserName = user.UserName,
            MfaEnabled = false, //As per the requirement
            SendUserRegistrationEmail = true
          };
          asyncTaskList.Add(_idamService.RegisterUserInIdamAsync(securityApiUserInfo));
          if (userNamePasswordIdentityProvider != null)
          {
            user.UserIdentityProviders.Add(new UserIdentityProvider()
            {
              UserId = user.Id,
              OrganisationEligibleIdentityProviderId = userNamePasswordIdentityProvider.Id,
              IsDeleted = false
            });
          }
        }

        var providerName = identityProviders.Where(x => !x.IsDeleted && availableIdps.Any(y => y == x.Id)).ToList();

        //asyncTaskList.Add(_ccsSsoEmailService.SendUserWelcomeEmailAsync(user.UserName, string.Join(",", recordsToDelete.Select(x => x.OrganisationEligibleIdentityProvider.IdentityProvider.IdpName))));
        var isFederatedIdp = providerName.Where(x => x.ExternalIdpFlag).FirstOrDefault();
        var isInternalIdp = providerName.Where(x => !x.ExternalIdpFlag).FirstOrDefault();

        if (isFederatedIdp != null && isInternalIdp != null)
        {
          string activationLink = "";
          await _ccsSsoEmailService.SendUserUpdateEmailBothIdpAsync(user.UserName, string.Join(",", providerName.Select(x => x.IdpName)), activationLink);
        }
        else if (isInternalIdp != null)
        {
          var activationLink = "";
          await _ccsSsoEmailService.SendUserUpdateEmailOnlyUserIdPwdAsync(user.UserName, activationLink);
        }
        else if (isFederatedIdp != null)
        {
          await _ccsSsoEmailService.SendUserUpdateEmailOnlyFederatedIdpAsync(user.UserName, string.Join(",", providerName.Select(x => x.IdpName)));
        }
        //Record for force signout as idp has been removed from the user. This is a current business requirement
        // asyncTaskList.Add(_remoteCacheService.SetValueAsync(CacheKeyConstant.ForceSignoutKey + user.UserName, true));
      });
      return asyncTaskList;
    }
  }
}

using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler
{
    public class UnverifiedUserDeleteJob : BackgroundService
    {
        private IDataContext _dataContext;
        private readonly IDateTimeService _dataTimeService;
        private readonly AppSettings _appSettings;
        private readonly IEmailSupportService _emailSupportService;
        private IOrganisationSupportService _organisationSupportService;
        private readonly IIdamSupportService _idamSupportService;
        private readonly IServiceProvider _serviceProvider;
        private IContactSupportService _contactSupportService;

        public UnverifiedUserDeleteJob(IServiceProvider serviceProvider, IDateTimeService dataTimeService,
          AppSettings appSettings, IEmailSupportService emailSupportService, IIdamSupportService idamSupportService)
        {
            _dataTimeService = dataTimeService;
            _appSettings = appSettings;
            _emailSupportService = emailSupportService;
            _idamSupportService = idamSupportService;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine("Unverified User Deletion Job");
                    _dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
                    _organisationSupportService = scope.ServiceProvider.GetRequiredService<IOrganisationSupportService>();
                    _contactSupportService = scope.ServiceProvider.GetRequiredService<IContactSupportService>();
                    await PerformJobAsync();
                    await Task.Delay(_appSettings.ScheduleJobSettings.UnverifiedUserDeletionJobExecutionFrequencyInMinutes * 60000, stoppingToken);
                }
            }
        }

        public void InitiateScopedServices(IDataContext dataContext, IOrganisationSupportService organisationSupportService)
        {
            _dataContext = dataContext;
            _organisationSupportService = organisationSupportService;
        }

        public async Task PerformJobAsync()
        {
            Dictionary<int, List<string>> adminList = new Dictionary<int, List<string>>();
            Dictionary<int, bool> orgSiteContactAvailabilityStatus = new Dictionary<int, bool>();
            var users = await GetUsersToDeleteAsync();

            var orgAdminList = new List<string>();
            foreach (var orgByUsers in users.GroupBy(u => u.Party.Person.OrganisationId))
            {
                Console.WriteLine($"Unverified User Deletion Organisation: {orgByUsers.Key}");
                foreach (var user in orgByUsers.Select(ou => ou).ToList())
                {
                    Console.WriteLine($"Unverified User Deletion User: {user.UserName} Id:{user.Id}");
                    try
                    {
                        ContactPoint reassigningContactPoint = null;
                        bool shouldDeleteInIdam = false;
                        user.IsDeleted = true;
                        user.Party.IsDeleted = true;
                        user.Party.Person.IsDeleted = true;

                        if (user.UserGroupMemberships != null)
                        {
                            user.UserGroupMemberships.ForEach((userGroupMembership) =>
                            {
                                userGroupMembership.IsDeleted = true;
                            });
                        }

                        if (user.UserAccessRoles != null)
                        {
                            user.UserAccessRoles.ForEach((userAccessRole) =>
                            {
                                userAccessRole.IsDeleted = true;
                            });
                        }

                        if (user.UserIdentityProviders != null)
                        {
                            shouldDeleteInIdam = user.UserIdentityProviders.Any(ui => !ui.IsDeleted
                              && ui.OrganisationEligibleIdentityProvider.IdentityProvider.IdpConnectionName == Contstant.ConclaveIdamConnectionName);
                            user.UserIdentityProviders.ForEach((idp) => { idp.IsDeleted = true; });
                        }

                        if (user.Party.ContactPoints != null && user.Party.ContactPoints.Any(cp => !cp.IsDeleted))
                        {
                            if (!orgSiteContactAvailabilityStatus.ContainsKey(orgByUsers.Key))
                            {
                                Console.WriteLine($"Unverified User Deletion User: {user.UserName} Setting orgSiteContactAvailabilityStatus");
                                var orgSiteContactExists = await _contactSupportService.IsOrgSiteContactExistsAsync(user.Party.ContactPoints.Where(cp => !cp.IsDeleted).Select(cp => cp.Id).ToList(), orgByUsers.Key);
                                orgSiteContactAvailabilityStatus.Add(orgByUsers.Key, orgSiteContactExists);
                                Console.WriteLine($"Unverified User Deletion User: {user.UserName} orgSiteContactAvailabilityStatus: {orgSiteContactExists}");
                            }

                            if (!orgSiteContactAvailabilityStatus[orgByUsers.Key])
                            {
                                Console.WriteLine($"Unverified User Deletion User: {user.UserName} NoOrgSiteContacts: {true}");
                                // User contact check should run for every user since other user can be deleted in net execution
                                var otherUserContactExists = await _contactSupportService.IsOtherUserContactExistsAsync(user.UserName, orgByUsers.Key);
                                if (!otherUserContactExists)
                                {
                                    Console.WriteLine($"Unverified User Deletion User: {user.UserName} reassigningContactPoint: {true}");
                                    // This is the last user contact of organisation. This will only run once per an organisation
                                    reassigningContactPoint = user.Party.ContactPoints.Where(cp => !cp.IsDeleted).OrderByDescending(cp => cp.CreatedOnUtc).FirstOrDefault();
                                    var organisation = await _dataContext.Organisation.Include(o => o.Party).Where(o => o.Id == orgByUsers.Key).FirstOrDefaultAsync();
                                    ContactPoint newOrgContactPoint = new ContactPoint
                                    {
                                        ContactPointReasonId = reassigningContactPoint.ContactPointReasonId,
                                        ContactDetailId = reassigningContactPoint.ContactDetailId,
                                        PartyId = organisation.PartyId,
                                        PartyTypeId = organisation.Party.PartyTypeId
                                    };
                                    _dataContext.ContactPoint.Add(newOrgContactPoint);
                                    orgSiteContactAvailabilityStatus[orgByUsers.Key] = false; // Set only user contact unavailability (false) for orgcontact status (Cannot set true because the user might get deleted in next round)
                                }
                            }

                            // Delete assigned contact points
                            var contactDetialsIds = user.Party.ContactPoints.Where(cp => !cp.IsDeleted).Select(cp => cp.ContactDetailId).ToList();
                            var assignedContactPoints = await _dataContext.ContactPoint
                              .Where(cd => !cd.IsDeleted && contactDetialsIds.Contains(cd.ContactDetailId) && cd.OriginalContactPointId != 0)
                              .ToListAsync();
                            Console.WriteLine($"Unverified User Deletion User: {user.UserName} assignedContactPoints: {JsonConvert.SerializeObject(assignedContactPoints.Select(c => c.Id).ToList())}");

                            assignedContactPoints.ForEach(assignedContactPoint => assignedContactPoint.IsDeleted = true);

                            user.Party.ContactPoints.Where(cp => !cp.IsDeleted).ToList().ForEach((cp) =>
                            {
                                cp.IsDeleted = true;
                                if (reassigningContactPoint == null || cp.Id != reassigningContactPoint.Id) // Delete the contact details and virtual address of not reassigning contact point
                                {
                                    cp.ContactDetail.IsDeleted = true;
                                    cp.ContactDetail.VirtualAddresses.ForEach((va) => { va.IsDeleted = true; });
                                }
                            });
                        }

                        await _dataContext.SaveChangesAsync();

                        Console.WriteLine($"Unverified User Deletion User: {user.UserName} reassigningContactPoint: {reassigningContactPoint?.Id}");


                        if (shouldDeleteInIdam)
                        {
                            await _idamSupportService.DeleteUserInIdamAsync(user.UserName);
                            Console.WriteLine($"Unverified User Deletion User in Auth0: {user.UserName}");
                        }
                        var userDeleteJobSetting = _appSettings.UserDeleteJobSettings.FirstOrDefault(ud => ud.ServiceClientId == user.CcsService?.ServiceClientId);

                        if (userDeleteJobSetting == null)
                        {
                            userDeleteJobSetting = _appSettings.UserDeleteJobSettings.FirstOrDefault(ud => ud.ServiceClientId == "ANY");
                        }

                        if (userDeleteJobSetting.NotifyOrgAdmin)
                        {
                            Console.WriteLine($"Unverified User Notify Admin for: {user.UserName}");
                            if (!adminList.ContainsKey(orgByUsers.Key))
                            {
                                var adminUsers = await _organisationSupportService.GetAdminUsersAsync(orgByUsers.Key);
                                adminList.Add(orgByUsers.Key, adminUsers.Where(au => au.AccountVerified).Select(au => au.UserName).ToList());
                            }
                            await _emailSupportService.SendUnVerifiedUserDeletionEmailToAdminAsync($"{user.Party.Person.FirstName} {user.Party.Person.LastName}",
                            user.UserName, adminList[orgByUsers.Key]);
                            Console.WriteLine($"Unverified User Notify Admin Success for: {user.UserName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error UnverifiedUserDeleteJob: {JsonConvert.SerializeObject(ex)}");
                    }
                }
            }

        }

        private async Task<List<User>> GetUsersToDeleteAsync()
        {
            var minimumThreshold = _appSettings.UserDeleteJobSettings.Min(udj => udj.UserDeleteThresholdInMinutes);

            var users = await _dataContext.User.Include(u => u.Party).ThenInclude(p => p.Person)
                                .Include(u => u.UserGroupMemberships)
                                .Include(u => u.UserAccessRoles)
                                .Include(u => u.CcsService)
                                .Include(u => u.UserIdentityProviders).ThenInclude(ui => ui.OrganisationEligibleIdentityProvider).ThenInclude(ui => ui.IdentityProvider)
                                .Include(u => u.Party).ThenInclude(p => p.ContactPoints).ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
                                .Where(u => !u.AccountVerified && !u.IsDeleted
                                && (!u.UserGroupMemberships.Any(ugm => !ugm.IsDeleted
                                && ugm.OrganisationUserGroup.GroupEligibleRoles.Any(ga => !ga.IsDeleted
                                && ga.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey))
                                && (!u.UserAccessRoles.Any(ur => !ur.IsDeleted
                                && ur.OrganisationEligibleRole.CcsAccessRole.CcsAccessRoleNameKey == Contstant.OrgAdminRoleNameKey)))
                                && u.CreatedOnUtc < _dataTimeService.GetUTCNow().AddMinutes(-minimumThreshold))
                                .Select(u => u).ToListAsync();

            return users;
        }
    }
}

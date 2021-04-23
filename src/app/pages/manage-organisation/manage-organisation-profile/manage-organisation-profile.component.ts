import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

import { slideAnimation } from 'src/app/animations/slide.animation';
import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { Organisation } from 'src/app/models/organisation';
import { ContactDetails, Address, ContactType } from 'src/app/models/contactDetail';
import { contactService } from 'src/app/services/contact/contact.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { ciiService } from 'src/app/services/cii/cii.service';
import { WrapperUserService } from 'src/app/services/wrapper/wrapper-user.service';
import { User } from 'src/app/models/user';
import { TokenService } from 'src/app/services/auth/token.service';
import { WebsiteService } from 'src/app/services/postgres/website.service';
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';
import { Role } from 'src/app/models/organisationGroup';
import { WrapperOrganisationGroupService } from 'src/app/services/wrapper/wrapper-org--group-service';
import { share } from 'rxjs/operators';
import { IdentityProvider } from 'src/app/models/identityProvider';
import { WrapperConfigurationService } from 'src/app/services/wrapper/wrapper-configuration.service';
import { WrapperOrganisationContactService } from 'src/app/services/wrapper/wrapper-org-contact-service';
import { OrganisationContactInfoList } from 'src/app/models/contactInfo';
import { WrapperOrganisationSiteService } from 'src/app/services/wrapper/wrapper-org-site-service';
import { OrganisationSite, OrganisationSiteInfoList, SiteGridInfo } from 'src/app/models/site';

@Component({
    selector: 'app-manage-organisation-profile',
    templateUrl: './manage-organisation-profile.component.html',
    styleUrls: ['./manage-organisation-profile.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Default
})
export class ManageOrganisationProfileComponent extends BaseComponent implements OnInit {

    org$: any;
    organisationId!: number;
    ciiOrganisationId: string;
    contactData: ContactDetails[];
    organisationAddress: Address;
    siteData: SiteGridInfo[];
    registries: any[];
    additionalIdentifiers: any[];
    contactTableHeaders = ['CONTACT_REASON', 'NAME', 'EMAIL', 'TELEPHONE_NUMBER'];
    contactColumnsToDisplay = ['contactReason', 'name', 'email', 'phoneNumber'];
    siteTableHeaders = ['SITE_NAME', 'STREET_ADDRESS', 'POSTAL_CODE', 'COUNTRY_CODE'];
    siteColumnsToDisplay = ['siteName', 'streetAddress', 'postalCode', 'countryCode'];
    registriesTableDisplayedColumns: string[] = ['authority', 'id', 'type', 'actions'];
    organisation$!: Observable<Organisation>;
    public idps$!: Observable<IdentityProvider[]>;
    public orgIdps$!: Observable<IdentityProvider[]>;

    constructor(private contactService: contactService, private websiteService: WebsiteService,
        private organisationService: OrganisationService, private ciiService: ciiService,
        private configWrapperService: WrapperConfigurationService, private wrapperService: WrapperUserService,
        private router: Router, private route: ActivatedRoute,
        protected uiStore: Store<UIState>, private readonly tokenService: TokenService,
        private organisationGroupService: WrapperOrganisationGroupService,
        private orgContactService: WrapperOrganisationContactService,
        private wrapperOrgSiteService: WrapperOrganisationSiteService) {
        super(uiStore);
        this.contactData = [];
        this.organisationAddress = {};
        this.siteData = [];
        this.registries = [];
        this.additionalIdentifiers = [];
        this.ciiOrganisationId = localStorage.getItem('cii_organisation_id') || '';
        // this.organisationId = JSON.parse(localStorage.getItem('organisation_id') + '');
        // this.organisation$ = this.organisationService.getById(this.organisationId);
    }

    ngOnInit() {
        const accesstoken = this.tokenService.getDecodedAccessToken();
        this.organisationService.getById(accesstoken.ciiOrgId).subscribe(org => {
            if (org) {
                this.organisationId = org.organisationId;
                this.organisationAddress = org.address;
                this.org$ = org;
                this.idps$ = this.configWrapperService.getIdentityProviders().pipe(share());
                this.idps$.subscribe({
                    next: response => {
                        if (response) {
                            this.orgIdps$ = this.organisationGroupService.getOrganisationIdentityProviders(accesstoken.ciiOrgId).pipe(share());
                            this.orgIdps$.subscribe({
                                next: data => {
                                    if (data) {
                                        response.forEach(idp => {
                                            // console.log(element);
                                            // idp.enabled = true;
                                            data.forEach(element => {
                                                if (idp.connectionName == element.connectionName) {
                                                    idp.enabled = true;
                                                }
                                            });
                                        });
                                    }
                                }
                            });
                        }
                    }
                });
                // this.organisation$ = this.organisationService.getById(this.organisationId);
                localStorage.setItem('organisation_id', this.organisationId + '');
                this.contactService.getContacts(org.organisationId).subscribe(data => {
                    if (data && data.length > 0) {
                        var orgContact = data.find(c => c.contactType == ContactType.Organisation);
                        if (orgContact && orgContact.address) {
                            this.organisationAddress = orgContact.address;
                        }
                    }
                });
                this.orgContactService.getOrganisationContacts(this.ciiOrganisationId).subscribe({
                    next: (orgContactListInfo: OrganisationContactInfoList) => {
                        if (orgContactListInfo != null) {
                            this.contactData = orgContactListInfo.contactsList;
                        }
                    },
                    error: (error: any) => {
                        console.log(error);
                    }
                });
            }
        });

        this.wrapperOrgSiteService.getOrganisationSites(this.ciiOrganisationId).subscribe({
            next: (orgContactListInfo: OrganisationSiteInfoList) => {
                if (orgContactListInfo != null) {
                    console.log(orgContactListInfo.sites);
                    
                    this.siteData = orgContactListInfo.sites.map((site: OrganisationSite) => {
                        let siteGridInfo: SiteGridInfo = {
                            siteId : site.details.siteId,
                            siteName : site.siteName,
                            streetAddress : site.address.streetAddress,
                            postalCode : site.address.postalCode,
                            countryCode : site.address.countryCode,
                            locality : site.address.locality,
                            region : site.address.region,
                        };
                        return siteGridInfo;
                    });
                }
            },
            error: (error: any) => {
                console.log(error);
            }
        });

        this.ciiService.getOrgs(accesstoken.ciiOrgId).subscribe(data => {
            if (data && data.length > 0) {
                localStorage.setItem('cii_registries', JSON.stringify(data));
                this.registries = data;
                this.additionalIdentifiers = data[0].additionalIdentifiers;
            }
        });

        // this.wrapperService.getUser(localStorage.getItem('user_name')+'').subscribe({
        //     next: (user: User) => {
        //         if (user != null) { 
        //             localStorage.setItem('cii_organisation_id', user.organisationId);
        //             this.ciiService.getOrgs(user.organisationId).subscribe(data => {
        //                 if (data && data.length > 0) {
        //                     this.additionalIdentifiers = data[0].additionalIdentifiers;
        //                     this.registries = data;
        //                     localStorage.setItem('cii_registries', JSON.stringify(data));
        //                 }
        //             });                  
        //         } else {
        //             console.log('no user found from wrapper service');
        //         }
        //     },
        //     error: (error: any) => {
        //         console.log(error);
        //     }
        // });
    }

    public onContactAddClick() {        
        let data = {
            'isEdit': false,
            'contactId': 0
        };
        this.router.navigateByUrl('manage-org/profile/contact-edit?data=' + JSON.stringify(data));
    }

    public onContactEditClick(contactDetail: ContactDetails) {
        let data = {
            'isEdit': true,
            'contactId': contactDetail.contactId
        };
        this.router.navigateByUrl('manage-org/profile/contact-edit?data=' + JSON.stringify(data));
    }

    public onSiteAddClick() {
        let data = {
            'isEdit': false,
            'siteId': 0
        };
        this.router.navigateByUrl('manage-org/profile/site/edit?data=' + JSON.stringify(data));
    }

    public onSiteEditClick(orgSite: SiteGridInfo) {
        let data = {
            'isEdit': true,
            'siteId': orgSite.siteId
        };
        this.router.navigateByUrl('manage-org/profile/site/edit?data=' + JSON.stringify(data));
    }

    public onRegistryAddClick() {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/search`);
    }

    public onRegistryEditClick(row: any) {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/${row.scheme}/${row.id}`);
    }

    public onRegistryRemoveClick(row: any) {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/delete/${row.scheme}/${row.id}`);
    }

    public onIdentityProviderChange(row: any) {
        const accesstoken = this.tokenService.getDecodedAccessToken();
        this.organisationGroupService.enableIdentityProvider(accesstoken.ciiOrgId, row.connectionName, !row.enabled).subscribe(data => {

        });
    }

    public onSaveChanges() {
        this.router.navigateByUrl(`manage-org/profile/success`);
    }

    public onCancel() {
        this.router.navigateByUrl(`home`);
    }

    public isPrimary(row: any): boolean {
        if (!this.registries) return true;
        let index = this.registries.indexOf(row);
        if (index === -1) {
            return false;
        }
        if (index >= 1) {
            return false;
        }
        return true;
    }

    public getSchemaName(schema: string): string {
        switch (schema) {
            case 'GB-COH': {
                return 'Companies House';
            }
            case 'US-DUN': {
                return 'Dun and Bradstreet';
            }
            case 'GB-CHC': {
                return 'Charities Commission for England and Wales';
            }
            case 'GB-SC': {
                return 'Scottish Charities Commission';
            }
            case 'GB-NIC': {
                return 'Northern Ireland Charities Commission';
            }
            default: {
                return '';
            }
        }
    }

}

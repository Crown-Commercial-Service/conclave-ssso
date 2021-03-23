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
    contactData: ContactDetails[];
    organisationAddress: Address;
    siteData: any[];
    registries: any[];
    additionalIdentifiers: any[];
    displayedColumns: string[] = ['name', 'email', 'teamName', 'phoneNumber', 'actions'];
    siteTableDisplayedColumns: string[] = ['name', 'createdDate', 'actions'];
    registriesTableDisplayedColumns: string[] = ['authority', 'id', 'type', 'actions'];
    organisation$!: Observable<Organisation>;
    
    constructor(private contactService: contactService, private websiteService: WebsiteService, private organisationService: OrganisationService, private ciiService: ciiService, private wrapperService: WrapperUserService, private wrapperOrgService: WrapperOrganisationService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService) {
        super(uiStore);
        this.contactData = [];
        this.organisationAddress = {};
        this.siteData = [];
        this.registries = [];
        this.additionalIdentifiers = [];
        // this.organisationId = JSON.parse(localStorage.getItem('organisation_id') + '');
        // this.organisation$ = this.organisationService.get(this.organisationId);
    }

    ngOnInit() {
        
        const accesstoken = this.tokenService.getDecodedAccessToken();

        this.organisationService.get(accesstoken.ciiOrgId).subscribe(org => {
            if (org){
                this.organisationId = org.organisationId;
                this.organisationAddress = org.address;
                this.org$ = org;
                // this.organisation$ = this.organisationService.get(this.organisationId);
                localStorage.setItem('organisation_id', this.organisationId+'');
                this.contactService.getContacts(org.organisationId).subscribe(data => {
                    if (data && data.length > 0){
                        var personContacts= data.filter(c=> c.contactType == ContactType.OrganisationPerson);
                        this.contactData = personContacts;
                        var orgContact = data.find(c => c.contactType== ContactType.Organisation);
                        if (orgContact && orgContact.address){
                            this.organisationAddress = orgContact.address;
                        }
                    }
                });
            }
        });

        this.wrapperOrgService.getSites(accesstoken.ciiOrgId).subscribe(data => {
            if (data){
                this.siteData = data.sites;
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
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-edit/0`);
    }

    public onContactEditClick(event: any, contactId: number = 0){
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-edit/${contactId}`);
    }

    public onSiteAddClick() {
        this.router.navigateByUrl(`manage-org/profile/site/add`);
    }

    public onSiteEditClick(row: any) {
        this.router.navigateByUrl(`manage-org/profile/site/edit/` + row.siteId);
    }

    public onRegistryAddClick() {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/search`);
    }

    public onRegistryEditClick(row: any) {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/${row.scheme}/${row.id}`);
    }

    public onRegistryRemoveClick(row: any){
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/delete/${row.scheme}/${row.id}`);
    }

    public isPrimary(row:any): boolean {
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
        switch(schema) { 
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

import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { slideAnimation } from "src/app/animations/slide.animation";
import { ActivatedRoute, Router } from "@angular/router";
import { AuthService } from "src/app/services/auth/auth.service";
import { OperationEnum } from "src/app/constants/enum";
import { WrapperUserContactService } from "src/app/services/wrapper/wrapper-user-contact.service";
import { WrapperOrganisationContactService } from "src/app/services/wrapper/wrapper-org-contact-service";
import { WrapperSiteContactService } from "src/app/services/wrapper/wrapper-site-contact-service";

@Component({
    selector: 'app-manage-organisation-contact-delete',
    templateUrl: './manage-organisation-contact-delete.component.html',
    styleUrls: ['./manage-organisation-contact-delete.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageOrganisationContactDeleteComponent extends BaseComponent implements OnInit {
    organisationId: string;
    contactId: number = 0;
    siteId: number = 0;
    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private contactService: WrapperOrganisationContactService, private siteContactService: WrapperSiteContactService) {
        super(uiStore);
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            console.log(routeData);
            this.contactId = routeData['contactId'];
            this.siteId = routeData['siteId'] || 0;
        }
    }

    ngOnInit() {
    }

    onDeleteConfirmClick() {
        if (this.siteId == 0){
            this.contactService.deleteOrganisationContact(this.organisationId, this.contactId).subscribe({
                next: () => { 
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.DeleteOrgContact}`);           
                },
                error: (error) => {
                    console.log(error);
                }
            });
        }
        else{
            this.siteContactService.deleteSiteContact(this.organisationId, this.siteId, this.contactId).subscribe({
                next: () => { 
                    let data = {
                        'siteId': this.siteId
                    };
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.DeleteSiteContact}?data=` + JSON.stringify(data));           
                },
                error: (error) => {
                    console.log(error);
                }
            });
        }
        
    }

    onCancelClick(){
        let data = {
            'isEdit': true,
            'contactId': this.contactId,
            'siteId': this.siteId
        };
        this.router.navigateByUrl('manage-org/profile/contact-edit?data=' + JSON.stringify(data));
    }
}
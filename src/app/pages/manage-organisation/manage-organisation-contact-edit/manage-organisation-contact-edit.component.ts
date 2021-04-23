import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { ViewportScroller } from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';
import { ContactInfo, OrganisationContactInfo, SiteContactInfo } from 'src/app/models/contactInfo';
import { ScrollHelper } from 'src/app/services/helper/scroll-helper.services';
import { ContactReason } from 'src/app/models/contactDetail';
import { WrapperConfigurationService } from 'src/app/services/wrapper/wrapper-configuration.service';
import { WrapperOrganisationContactService } from 'src/app/services/wrapper/wrapper-org-contact-service';
import { WrapperSiteContactService } from 'src/app/services/wrapper/wrapper-site-contact-service';

@Component({
    selector: 'app-manage-organisation-contact-edit',
    templateUrl: './manage-organisation-contact-edit.component.html',
    styleUrls: ['./manage-organisation-contact-edit.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageOrganisationContactEditComponent extends BaseComponent implements OnInit {

    organisationId: string = '';
    contactData: ContactInfo;
    contactForm: FormGroup;
    submitted!: boolean;
    contactReasonLabel: string = "CONTACT_REASON";
    default: string = '';
    contactReasons: ContactReason[] = [];
    isEdit: boolean = false;
    contactId: number = 0;
    siteId: number = 0;

    @ViewChildren('input') inputs!: QueryList<ElementRef>;

    constructor(private contactService: WrapperOrganisationContactService, private formBuilder: FormBuilder, private router: Router,
        private activatedRoute: ActivatedRoute, protected uiStore: Store<UIState>,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper,
        private configurationService: WrapperConfigurationService, private siteContactService: WrapperSiteContactService) {
        super(uiStore);
        this.contactData = {};
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            this.isEdit = routeData['isEdit'];
            this.contactId = routeData['contactId'];
            this.siteId = routeData['siteId'] || 0;
        }
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.contactForm = this.formBuilder.group({
            name: ['', Validators.compose([])],
            email: ['', Validators.compose([Validators.email])],
            phone: ['', Validators.compose([])],
            fax: ['', Validators.compose([])],
            webUrl: ['', Validators.compose([])],
            contactReason: ['', Validators.compose([])],
        }, { validators: this.validateForSufficientDetails });
        this.contactForm.controls['contactReason'].setValue(this.default, { onlySelf: true });
    }

    ngOnInit() {
        this.configurationService.getContactReasons().subscribe({
            next: (contactReasons: ContactReason[]) => {
                if (contactReasons != null) {
                    this.contactReasons = contactReasons;
                    if (this.isEdit) {
                        if (this.siteId == 0) {
                            this.getOrganisationContact();
                        }
                        else {
                            this.getSiteContact();
                        }
                    }
                }
            },
            error: (error: any) => {
                console.log(error);
            }
        });
    }

    getOrganisationContact() {
        this.contactService.getOrganisationContactById(this.organisationId, this.contactId).subscribe({
            next: (contactInfo: OrganisationContactInfo) => {
                console.log(contactInfo);
                this.contactForm.controls['name'].setValue(contactInfo.name);
                this.contactForm.controls['email'].setValue(contactInfo.email);
                this.contactForm.controls['phone'].setValue(contactInfo.phoneNumber);
                this.contactForm.controls['fax'].setValue(contactInfo.fax);
                this.contactForm.controls['webUrl'].setValue(contactInfo.webUrl);
                this.contactForm.controls['contactReason'].setValue(contactInfo.contactReason);
            },
            error: (error: any) => {
                console.log(error);
            }
        });
    }

    getSiteContact() {
        this.siteContactService.getSiteContactById(this.organisationId, this.siteId, this.contactId).subscribe({
            next: (contactInfo: SiteContactInfo) => {
                this.contactForm.controls['name'].setValue(contactInfo.name);
                this.contactForm.controls['email'].setValue(contactInfo.email);
                this.contactForm.controls['phone'].setValue(contactInfo.phoneNumber);
                this.contactForm.controls['fax'].setValue(contactInfo.fax);
                this.contactForm.controls['webUrl'].setValue(contactInfo.webUrl);
                this.contactForm.controls['contactReason'].setValue(contactInfo.contactReason);
            },
            error: (error: any) => {
                console.log(error);
            }
        });
    }

    ngAfterViewChecked() {
        this.scrollHelper.doScroll();
    }

    scrollToAnchor(elementId: string): void {
        this.viewportScroller.scrollToAnchor(elementId);
    }

    setFocus(inputIndex: number) {
        this.inputs.toArray()[inputIndex].nativeElement.focus();
    }

    validateForSufficientDetails(form: FormGroup) {
        let name = form.get('name')?.value;
        let email = form.get('email')?.value;
        let phone = form.get('phone')?.value;
        let fax = form.get('fax')?.value;
        let web = form.get('webUrl')?.value;

        return !name && !email && !phone && !fax && !web ? { inSufficient: true } : null;
    }

    public onSubmit(form: FormGroup) {
        this.submitted = true;
        if (this.formValid(form)) {

            this.contactData.name = form.get('name')?.value;
            this.contactData.email = form.get('email')?.value;
            this.contactData.phoneNumber = form.get('phone')?.value;
            this.contactData.fax = form.get('fax')?.value;
            this.contactData.webUrl = form.get('webUrl')?.value;
            this.contactData.contactReason = form.get('contactReason')?.value;

            if (this.siteId == 0) { // If organisation contact
                if (this.isEdit) {
                    this.updateOrgContact(form);
                }
                else {
                    this.createOrgContact(form);
                }
            }
            else { // If site contact
                if (this.isEdit) {
                    this.updateSiteContact(form);
                }
                else {
                    this.createSiteContact(form);
                }
            }
        }
        else {
            this.scrollHelper.scrollToFirst('error-summary');
        }
    }

    updateOrgContact(form: FormGroup) {
        this.contactService.updateOrganisationContact(this.organisationId, this.contactId, this.contactData)
            .subscribe({
                next: () => {
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.UpdateOrgContact}`);
                    this.submitted = false;
                },
                error: (error) => {
                    console.log(error);
                    this.setError(form, error.error);
                }
            });
    }

    createOrgContact(form: FormGroup) {
        this.contactService.createOrganisationContact(this.organisationId, this.contactData)
            .subscribe({
                next: () => {
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.CreateOrgContact}`);
                    this.submitted = false;
                },
                error: (error) => {
                    console.log(error);
                    this.setError(form, error.error);
                }
            });
    }

    updateSiteContact(form: FormGroup) {
        this.siteContactService.updateSiteContact(this.organisationId, this.siteId, this.contactId, this.contactData)
            .subscribe({
                next: () => {
                    let data = {
                        'siteId': this.siteId
                    };
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.UpdateSiteContact}?data=` + JSON.stringify(data));
                    this.submitted = false;
                },
                error: (error) => {
                    console.log(error);
                    this.setError(form, error.error);
                }
            });
    }

    createSiteContact(form: FormGroup) {
        this.siteContactService.createSiteContact(this.organisationId, this.siteId, this.contactData)
            .subscribe({
                next: () => {
                    let data = {
                        'siteId': this.siteId
                    };
                    this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.CreateSiteContact}?data=` + JSON.stringify(data));
                    this.submitted = false;
                },
                error: (error) => {
                    console.log(error);
                    this.setError(form, error.error);
                }
            });
    }

    setError(form: FormGroup, errorCode: string) {
        let errorString: string = '';
        let control: string = '';
        if (errorCode == "INVALID_PHONE_NUMBER") {
            control = 'phone';
            errorString = 'invalid';
        }
        else if (errorCode == "INVALID_EMAIL") {
            control = 'email';
            errorString = 'email';
        }
        var errorObject: ValidationErrors = {};
        errorObject[errorString] = true;
        form.controls[control].setErrors(errorObject);
        this.scrollHelper.scrollToFirst('error-summary');
    }

    formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        return form.valid;
    }

    onCancelClick() {
        if (this.siteId == 0) {
            this.router.navigateByUrl('manage-org/profile');
        }
        else {
            let data = {
                'isEdit': true,
                'siteId': this.siteId
            };
            this.router.navigateByUrl('manage-org/profile/site/edit?data=' + JSON.stringify(data));
        }
    }

    onDeleteClick() {
        let data = {
            'organisationId': this.organisationId,
            'contactId': this.contactId,
            'siteId': this.siteId
        };
        this.router.navigateByUrl('manage-org/profile/contact-delete?data=' + JSON.stringify(data));
    }
}

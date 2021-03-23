import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Location } from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { ContactDetails, ContactType } from 'src/app/models/contactDetail';
import { contactService } from 'src/app/services/contact/contact.service';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';

@Component({
    selector: 'app-manage-organisation-contact-edit',
    templateUrl: './manage-organisation-contact-edit.component.html',
    styleUrls: ['./manage-organisation-contact-edit.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Default
})
export class ManageOrganisationContactEditComponent extends BaseComponent implements OnInit {

    organisationId: number;
    contactId: number;
    contactData: ContactDetails;
    contactForm: FormGroup;
    submitted!: boolean;

    constructor(private contactService: contactService, private formBuilder: FormBuilder, private router: Router,
        private location: Location, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
        super(uiStore);
        this.contactData = {};
        this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId') || '0');
        this.contactId = parseInt(this.route.snapshot.paramMap.get('contactId') || '0');
        this.contactForm = this.formBuilder.group({
            name: ['', Validators.compose([Validators.required])],
            email: ['', Validators.compose([Validators.email])],
            phone: ['', Validators.compose([])]
        });
    }

    ngOnInit() {
        if (this.contactId != 0) {
            this.contactService.getContact(this.contactId)
                .subscribe(data => {
                    this.contactData = data || {};
                    if (data != null) {
                        this.contactForm.setValue({
                            "name": data.name,
                            "email": data.email,
                            "phone": data.phoneNumber
                        });
                    }
                });
        }
    }

    public onSubmit(form: FormGroup) {
        console.log('submit');
        this.submitted = true;
        if (this.formValid(form)) {
            console.log('valid');
            this.submitted = false;

            this.contactData.name = form.get('name')?.value;
            this.contactData.email = form.get('email')?.value;
            this.contactData.phoneNumber = form.get('phone')?.value;

            if (this.contactId != 0) {
                this.contactService.updateContact(this.contactData.contactId || 0, this.contactData)
                    .subscribe(
                        (data) => {
                            this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-operation-success/${OperationEnum.Update}`);
                        },
                        (error) => {
                            console.log(error);
                            console.log(error.error);
                        });
            }
            else {
                this.contactData.organisationId = this.organisationId;
                this.contactData.contactType = ContactType.OrganisationPerson;
                this.contactService.createContact(this.contactData)
                    .subscribe(
                        (data) => {
                            this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-operation-success/${OperationEnum.Create}`);
                        },
                        (error) => {
                            console.log(error);
                            console.log(error.error);
                        });
            }
        }
    }

    public formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        return form.valid;
    }

    public onCancelClick() {
        this.location.back();
    }

    public onDeleteClick(event: any) {
        this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-delete/${this.contactId}`);
    }
}

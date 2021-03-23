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
import { ciiService } from 'src/app/services/cii/cii.service';

@Component({
    selector: 'app-manage-organisation-profile-registry',
    templateUrl: './manage-organisation-profile-registry.component.html',
    styleUrls: ['./manage-organisation-profile-registry.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Default
})
export class ManageOrganisationRegistryComponent extends BaseComponent implements OnInit {

    organisationId: number;
    ccsOrgId!: string;
    scheme: string;
    id: string;
    frmGroup: FormGroup;
    submitted!: boolean;

    constructor(private ciiService: ciiService, private formBuilder: FormBuilder, private router: Router,
        private location: Location, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
        super(uiStore);
        this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId')+'' || '0');
        // this.ccsOrgId = this.route.snapshot.paramMap.get('ccsOrgId')+'';
        this.scheme = this.route.snapshot.paramMap.get('scheme')+'';
        this.id = this.route.snapshot.paramMap.get('id')+'';
        this.frmGroup = this.formBuilder.group({
            // ccsOrgId: [this.ccsOrgId, Validators.compose([Validators.required])],
            scheme: [this.scheme === 'null' ? '' : this.scheme, Validators.compose([Validators.required])],
            id: [this.id === 'null' ? '' : this.id, Validators.compose([Validators.required])]
        });
    }

    ngOnInit() {

    }

    public onSubmit(form: FormGroup) {
        console.log('submit');
        this.submitted = true;
        if (this.formValid(form)) {
            console.log('valid');
            this.submitted = false;
            const json = {
                ccsOrgId: '848881615464230034',
                identifier: {
                    scheme: form.get('scheme')?.value,
                    id: form.get('id')?.value,
                }
            };
            this.ciiService.updateOrganisation(JSON.stringify(json)).subscribe((data) => {
                this.router.navigateByUrl(`manage-org/profile/${this.organisationId}`);
            },
            (error) => {
                console.log(error);
            });
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
        // this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-delete/${this.contactId}`);
    }

    public isNew() : boolean {
        if (this.id === 'null' && this.scheme === 'null') {
            return true;
        }
        return false;
    }
}

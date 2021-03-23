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
import { TokenService } from 'src/app/services/auth/token.service';
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';

@Component({
    selector: 'app-manage-organisation-profile-site-add',
    templateUrl: './manage-organisation-profile-site-add.component.html',
    styleUrls: ['./manage-organisation-profile-site-add.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationSiteAddComponent extends BaseComponent implements OnInit {

    formGroup: FormGroup;
    submitted!: boolean;

    constructor(private contactService: contactService, private formBuilder: FormBuilder, private router: Router,
        private location: Location, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService,  private wrapperOrgService: WrapperOrganisationService) {
        super(uiStore);
        this.formGroup = this.formBuilder.group({
            name: ['', Validators.compose([Validators.required])],
            streetAddress: ['', Validators.compose([Validators.required])],
            locality: ['', Validators.compose([Validators.required])],
            region: ['', null],
            postalCode: ['', Validators.compose([Validators.required])],
            countryCode: ['', Validators.compose([Validators.required])],
        });
    }

    ngOnInit() {

    }

    public onSubmit(form: FormGroup) {
        this.submitted = true;
        if (this.formValid(form)) {
            console.log('valid');
            this.submitted = false;
            const accesstoken = this.tokenService.getDecodedAccessToken();
            const json = {
                siteName: form.get('name')?.value,
                streetAddress: form.get('streetAddress')?.value,
                locality: form.get('locality')?.value,
                region: form.get('region')?.value,
                postalCode: form.get('postalCode')?.value,
                countryCode: form.get('countryCode')?.value,
            };
            this.wrapperOrgService.addSite(accesstoken.ciiOrgId, JSON.stringify(json)).subscribe(data => {
              this.router.navigateByUrl(`manage-org/profile/site/add/success`);
            });
            // this.router.navigateByUrl(`manage-org/profile/site/add/success`);
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
        // this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-delete/${this.id}`);
    }
}

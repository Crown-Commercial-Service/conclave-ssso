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
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';
import { TokenService } from 'src/app/services/auth/token.service';

@Component({
    selector: 'app-manage-organisation-profile-site-edit',
    templateUrl: './manage-organisation-profile-site-edit.component.html',
    styleUrls: ['./manage-organisation-profile-site-edit.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationSiteEditComponent extends BaseComponent implements OnInit {

    id: number;
    formGroup: FormGroup;
    submitted!: boolean;
    site!: any;

    constructor(private contactService: contactService, private formBuilder: FormBuilder, private router: Router, private wrapperOrgService: WrapperOrganisationService, 
        private location: Location, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService) {
        super(uiStore);
        this.id = parseInt(this.route.snapshot.paramMap.get('id') || '0');
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
      const accesstoken = this.tokenService.getDecodedAccessToken();
      this.wrapperOrgService.getSite(accesstoken.ciiOrgId, this.id).subscribe(data => {
        if (data){
          if (data != null) {
            this.site = data;
            this.formGroup.setValue({
                "name": data.siteName,
                "streetAddress": data.streetAddress,
                "locality": data.locality,
                "region": data.region,
                "postalCode": data.postalCode,
                "countryCode": data.countryCode,
            });
          }
        }
      });
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
            this.wrapperOrgService.updateSite(accesstoken.ciiOrgId, this.id, JSON.stringify(json)).subscribe(data => {
              this.router.navigateByUrl(`manage-org/profile/site/edit/success`);
            });
            // this.router.navigateByUrl(`manage-org/profile/site/edit/success`);
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

    public onDeleteClick() {
      this.router.navigateByUrl(`manage-org/profile/site/delete/${this.id}`);
    }
}

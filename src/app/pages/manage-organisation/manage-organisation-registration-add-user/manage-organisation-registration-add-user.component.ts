import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError, map, timeout } from 'rxjs/operators';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { Scheme } from '../../../models/scheme';
import { UIState } from 'src/app/store/ui.states';
import { AuthService } from 'src/app/services/auth/auth.service';
import { ciiService } from 'src/app/services/cii/cii.service';
import { UserService } from 'src/app/services/postgres/user.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { contactService } from 'src/app/services/contact/contact.service';
import { ContactType } from 'src/app/models/contactDetail';

@Component({
  selector: 'app-manage-organisation-registration-add-user',
  templateUrl: './manage-organisation-registration-add-user.component.html',
  styleUrls: ['./manage-organisation-registration-add-user.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrgRegAddUserComponent extends BaseComponent implements OnInit {

  formGroup: FormGroup;
  submitted: boolean = false;

  constructor(private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.formGroup = this.formBuilder.group({
      firstName: [localStorage.getItem("new_user_firstName"), Validators.compose([Validators.required])],
      lastName: [localStorage.getItem("new_user_lastName"), Validators.compose([Validators.required])],
      // userName: ['', Validators.compose([Validators.required])],
      email: [localStorage.getItem("new_user_email"), Validators.compose([Validators.required, Validators.email])],
      title: ['Mr', Validators.compose([Validators.required])],
      ccsEmails: ['', Validators.compose([Validators.email])]
    });
  }

  ngOnInit() {

  }

  public onSubmit(form: FormGroup) {
    this.submitted = true;
    if (this.formValid(form)) {
      this.userService.getUser(form.get('email')?.value).toPromise().then((resp: any) => {
        // this.router.navigateByUrl(`manage-org/register/error/reg-id-exists`);
        localStorage.setItem("new_user_firstName", form.get('firstName')?.value);
        localStorage.setItem("new_user_lastName", form.get('lastName')?.value);
        localStorage.setItem("new_user_email", form.get('email')?.value);
        window.location.href = '/manage-org/register/error/username';
      }, (error: any) => {
        console.log(error);
        localStorage.removeItem("new_user_firstName");
        localStorage.removeItem("new_user_lastName");
        localStorage.removeItem("new_user_email");
        // if (error.status === 404) {
        //   this.router.navigateByUrl(`manage-org/register/error/reg-id-exists`);
        // }
        const organisation = localStorage.getItem('cii_organisation');
        const item$ = this.ciiService.addOrganisation(organisation);
        item$.subscribe({
          next: result => {
            console.log('---------CII ADD ORG RESPONSE START---------');
            console.log(result);
            console.log('---------CII ADD ORG RESPONSE FINISH--------');
            if (result.error) {
              this.router.navigateByUrl(`manage-org/register/error/reg-id-exists`);
            }
            if (result.identifier) {
              this.router.navigateByUrl(`manage-org/register/error/reg-id-exists`);
            }
            const org = JSON.parse(organisation ? organisation : '');
            if (org) {
              org.ccsOrgId = result.response.ccsOrgId;
              localStorage.setItem('ccs_organisation_id', JSON.stringify(org.ccsOrgId));
              this.organisationService.add(org).toPromise().then((orgResponse) => {
                console.log(orgResponse);
                localStorage.setItem('organisation_id', JSON.stringify(orgResponse));
                const user = {
                  firstName: form.get('firstName')?.value,
                  lastName: form.get('lastName')?.value,
                  userName: form.get('email')?.value,
                  title: form.get('title')?.value,
                  orgId: JSON.stringify(orgResponse),
                };
                const physicalContact = {
                  address: org.address,
                  contactType: ContactType.Organisation,
                  organisationId: orgResponse,
                };
                this.contactService.createContact(physicalContact).toPromise().then((contactResponse) => {
                  console.log('---------ADD PHYSICAL CONTACT RESPONSE START---------');
                  console.log(contactResponse);
                  console.log('---------ADD PHYSICAL CONTACT RESPONSE FINISH--------');
                }, (err) => {
                  console.log(err);
                });
                if (org.contactPoint.email.length > 0 || org.contactPoint.name.length > 0 || org.contactPoint.faxNumber.length > 0 || org.contactPoint.telephone.length > 0  || org.contactPoint.uri.length > 0) {
                  const contact = {
                    contactType: 1,
                    email: org.contactPoint.email,
                    fax: org.contactPoint.faxNumber,
                    name: org.contactPoint.name,
                    organisationId: orgResponse,
                    phoneNumber: org.contactPoint.telephone,
                  };
                  this.contactService.createContact(contact).toPromise().then((contactResponse) => {
                    console.log('---------ADD CONTACT RESPONSE START---------');
                    console.log(contactResponse);
                    console.log('---------ADD CONTACT RESPONSE FINISH--------');
                  }, (err) => {
                    console.log(err);
                  });
                }
                this.userService.add(user).toPromise().then((userResponse) => {
                  console.log('---------USER RESPONSE START---------');
                  console.log(userResponse);
                  console.log('---------USER RESPONSE FINISH--------');
                  this.authService.register(form.get('firstName')?.value, form.get('lastName')?.value, form.get('email')?.value, form.get('email')?.value).toPromise().then((registerResponse: any) => {
                    console.log('---------REGISTER RESPONSE START---------');
                    console.log(registerResponse);
                    console.log('---------REGISTER RESPONSE FINISH--------');
                    localStorage.setItem('brickendon_org_reg_email_address', form.get('email')?.value);
                    this.router.navigateByUrl(`manage-org/register/confirm`);
                  }, (err) => {
                    console.log(err);
                    this.router.navigateByUrl(`manage-org/register/error/generic`);
                  });
                });
              }, (err) => {
                console.log(err);
                this.router.navigateByUrl(`manage-org/register/error/generic`);
              });
            }
          },
          error: err => {
            if(err.status == 400) {
              this.router.navigateByUrl(`manage-org/register/error/notfound`);
            } else if(err.status == 404) {
              this.router.navigateByUrl(`manage-org/register/error/notfound`);
            } else if(err.status == 405) {
              this.router.navigateByUrl(`manage-org/register/error/reg-id-exists`);
            } else {
              this.router.navigateByUrl(`manage-org/register/error/generic`);
            }
          },
        });
      }
      );
    }
  }

  /**
    * iterate through each form control and validate
    */
  public formValid(form: FormGroup): Boolean {
    if (form == null) return false;
    if (form.controls == null) return false;
    return form.valid;
    // let array = _.takeWhile(form.controls, function(c:FormControl) { return !c.valid; });
    // let array = _.takeWhile([], function(c:FormControl) { return !c.valid; });
    // return array.length > 0;
  }

}

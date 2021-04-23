import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { UIState } from 'src/app/store/ui.states';
import { AuthService } from 'src/app/services/auth/auth.service';
import { ciiService } from 'src/app/services/cii/cii.service';
import { UserService } from 'src/app/services/postgres/user.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { contactService } from 'src/app/services/contact/contact.service';
import { ContactType } from 'src/app/models/contactDetail';
import { environment } from "src/environments/environment";
import { Observable } from 'rxjs';
import { filter, map, share } from 'rxjs/operators';
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';
import { WrapperUserService } from 'src/app/services/wrapper/wrapper-user.service';

@Component({
  selector: 'app-org-support-confirm-reset-password',
  templateUrl: './confirm.component.html',
  styleUrls: ['./confirm.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgSupportConfirmResetPasswordComponent extends BaseComponent implements OnInit {

  public user$!: Observable<any>;
  public user: any;

  constructor(private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private wrapperOrgService: WrapperOrganisationService, private wrapperUserService: WrapperUserService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params.id) {
        this.user$ = this.wrapperUserService.getUser(params.id).pipe(share());
        this.user$.subscribe({
          next: result => {
            this.user = result;
          }
        });
      }
    });
  }

  public onSubmitClick() {
    // this.org.rightToBuy = this.verified;
    this.authService.resetPassword(this.user.detail.userName).toPromise().then(() => {
       this.router.navigateByUrl(`org-support/success-reset-password/${this.user.detail.userName}`);
    });
  }

  public onCancelClick() {
    this.router.navigateByUrl('org-support/search');
  }
}

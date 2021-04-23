import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
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
import { TokenService } from 'src/app/services/auth/token.service';
import { Role } from 'src/app/models/organisationGroup';
import { WrapperOrganisationGroupService } from 'src/app/services/wrapper/wrapper-org--group-service';
import { WrapperConfigurationService } from 'src/app/services/wrapper/wrapper-configuration.service';

@Component({
  selector: 'app-buyer-confirm-changes',
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
export class BuyerConfirmChangesComponent extends BaseComponent {

  public org: any;
  public org$!: Observable<any>;
  public changes: any;
  
  constructor(private cf: ChangeDetectorRef, private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private wrapperOrgService: WrapperOrganisationService, private wrapperConfigService: WrapperConfigurationService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService, private organisationGroupService: WrapperOrganisationGroupService) {
    super(uiStore);
    this.route.params.subscribe(params => {
      if (params.id) {
        this.org$ = this.organisationService.getById(params.id).pipe(share());
        this.org$.subscribe({
          next: data => {
            this.org = data;
            this.changes = JSON.parse(sessionStorage.getItem(`mse_org_${this.org.ciiOrganisationId}`)+'');
          }
        });
      }
    });
  }

  public onSubmitClick() {
    const model = {
      isBuyer: this.changes.rightToBuy,
      rolesToDelete: this.changes.toDelete,
      rolesToAdd: this.changes.toAdd,
    };
    this.wrapperOrgService.updateOrgRoles(this.org.ciiOrganisationId, JSON.stringify(model)).toPromise().then(() => {
      this.router.navigateByUrl(`buyer/success`);
    });
  }

  public onCancelClick() {
    this.router.navigateByUrl('buyer/search');
  }

  public onBackClick() {
    this.router.navigateByUrl('buyer/confirm/' + this.org.ciiOrganisationId);
  }
}

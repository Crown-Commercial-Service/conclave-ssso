import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
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
  selector: 'app-buyer-confirm',
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
export class BuyerConfirmComponent extends BaseComponent implements OnInit {

  public org: any;
  public org$!: Observable<any>;
  public verified: boolean = false;
  userProfileForm: FormGroup;
  orgRoles: Role[];
  roles: Role[];
  public orgEligableRoles$!: Observable<Role[]>;
  public orgEligableRoles2$!: Observable<Role[]>;
  public orgRoles$!: Observable<Role[]>;
  eRoles: Role[];
  rolesToAdd: Role[];
  rolesToDelete: Role[];

  @ViewChild('isBuyerTrue') isBuyerTrue!: ElementRef;
  @ViewChild('isBuyerFalse') isBuyerFalse!: ElementRef;

  constructor(private cf: ChangeDetectorRef, private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private wrapperOrgService: WrapperOrganisationService, private wrapperConfigService: WrapperConfigurationService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService, private organisationGroupService: WrapperOrganisationGroupService) {
    super(uiStore);
    this.orgRoles = [];
    this.eRoles = [];
    this.roles = [];
    this.userProfileForm = this.formBuilder.group({
      userTitle: ['', null],
    });
    this.rolesToAdd = [];
    this.rolesToDelete = [];
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params.id) {
        this.org$ = this.organisationService.getById(params.id).pipe(share());
        this.org$.subscribe({
          next: data => {
            this.org = data;
            this.verified = data.rightToBuy;
            this.getOrgRoles();
          }
        });
      }
    });
  }

  public onSelect(verified: boolean) {
    this.verified = verified;
    this.rolesToAdd = [];
    this.rolesToDelete = [];

    if (verified) {
      this.isBuyerTrue.nativeElement.checked = true;
      this.isBuyerFalse.nativeElement.checked = false;
    } else {
      this.isBuyerTrue.nativeElement.checked = false;
      this.isBuyerFalse.nativeElement.checked = true;
    }

    if (verified && !this.org.rightToBuy) {
      const currentRoles = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 0 && x.tradeEligibility !== 0 && x.orgTypeEligibility !== 0);
      const previousRoles = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 0 && x.tradeEligibility !== 1 && x.orgTypeEligibility !== 0);
      previousRoles.forEach((r) => {
        const match = currentRoles.includes(r);
        if (!match) {
          this.rolesToDelete.push(r);
        }
      });
      currentRoles.forEach((r) => {
        const match = previousRoles.includes(r);
        if (!match) {
          this.rolesToAdd.push(r);
        }
      });
      const currentRoles2 = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 1 && x.tradeEligibility !== 0 && x.orgTypeEligibility !== 0);
      const previousRoles2 = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 1 && x.tradeEligibility !== 1 && x.orgTypeEligibility !== 0);
      previousRoles2.forEach((r) => {
        const match = currentRoles2.includes(r);
        if (!match) {
          this.rolesToDelete.push(r);
        }
      });
      currentRoles2.forEach((r) => {
        const match = previousRoles2.includes(r);
        if (!match) {
          this.rolesToAdd.push(r);
        }
      });
      console.log('to insert:');
      console.log(this.rolesToAdd);
      console.log('to remove:');
      console.log(this.rolesToDelete);
    } else if (!verified && this.org.rightToBuy) {
      const currentRoles = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 0 && x.tradeEligibility !== 1 && x.orgTypeEligibility !== 0);
      const previousRoles = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 0 && x.tradeEligibility !== 0 && x.orgTypeEligibility !== 0);
      currentRoles.forEach((r) => {
        const match = previousRoles.includes(r);
        if (!match) {
          this.rolesToAdd.push(r);
        }
      });
      previousRoles.forEach((r) => {
        const match = currentRoles.includes(r);
        if (!match) {
          this.rolesToDelete.push(r);
        }
      });
      const currentRoles2 = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 1 && x.tradeEligibility !== 1 && x.orgTypeEligibility !== 0);
      const previousRoles2 = this.roles.filter(x => x.enabled && x.subscriptionTypeEligibility === 1 && x.tradeEligibility !== 0 && x.orgTypeEligibility !== 0);
      currentRoles2.forEach((r) => {
        const match = previousRoles2.includes(r);
        if (!match) {
          this.rolesToAdd.push(r);
        }
      });
      previousRoles2.forEach((r) => {
        const match = currentRoles2.includes(r);
        if (!match) {
          this.rolesToDelete.push(r);
        }
      });
      console.log('to insert:');
      console.log(this.rolesToAdd);
      console.log('to remove:');
      console.log(this.rolesToDelete);
    }
  }

  onChange(event: any, defaultValue: any, role: any) {
    if (defaultValue === true && !event.target.checked) {
      this.rolesToDelete.push(role);
    }
    else if (defaultValue == true && event.target.checked) {
      const index = this.rolesToDelete.indexOf(role);
      if (index > -1) {
        this.rolesToDelete.splice(index, 1);
      }
    }
    if (defaultValue === false && event.target.checked) {
      this.rolesToAdd.push(role);
    }
    else if (defaultValue == false && !event.target.checked) {
      const index = this.rolesToAdd.indexOf(role);
      if (index > -1) {
        this.rolesToAdd.splice(index, 1);
      }
    }
    // console.log('TO ADD');
    // console.log(this.rolesToAdd);
    // console.log('TO DELETE');
    // console.log(this.rolesToDelete);
  }

  public onSubmitClick() {
    let selection = {
      org: this.org,
      toDelete: this.rolesToDelete,
      toAdd: this.rolesToAdd,
      rightToBuy: this.verified,
      hasChanges: (this.org.rightToBuy === this.verified && this.rolesToAdd.length === 0 && this.rolesToDelete.length === 0) ? false : true
    };
    //selection.org.rightToBuy = this.verified;
    sessionStorage.setItem(`mse_org_${this.org.ciiOrganisationId}`, JSON.stringify(selection));
    this.router.navigateByUrl(`buyer/confirm-changes/${this.org.ciiOrganisationId}`);
    // this.organisationService.put(this.org).toPromise().then(() => {
    //   this.router.navigateByUrl(`buyer/success`);
    // });
    // this.organisationService.put(this.org).subscribe({
    //   next: data => {
    //     this.router.navigateByUrl(`buyer/success`);
    //   }
    // });
  }

  public onCancelClick() {
    sessionStorage.removeItemItem(`mse_org_${this.org.ciiOrganisationId}`);
    this.router.navigateByUrl('buyer/search');
  }

  getOrgRoles(){
    this.orgRoles$ = this.wrapperConfigService.getRoles().pipe(share());
    this.orgRoles$.subscribe({
      next: (orgRoles: Role[]) => {
        this.roles = orgRoles;
        this.orgEligableRoles$ = this.organisationGroupService.getEligableRoles(this.org.ciiOrganisationId).pipe(share());
        this.orgEligableRoles$.subscribe({
          next: (eRoles: Role[]) => {
            this.roles.forEach((r) => {
              r.enabled = eRoles.some(x => x.roleName == r.roleName);
            });
            this.eRoles = eRoles;
            setTimeout(() => {
              this.cf.detectChanges();
            }, 100);
          },
          error: (err: any) => {
            console.log(err)
          }
        });
        this.orgEligableRoles2$ = this.organisationGroupService.getEligableRoles(this.org.ciiOrganisationId).pipe(share());
        this.orgEligableRoles2$.subscribe({
          next: (eRoles: Role[]) => {
            setTimeout(() => {
              this.cf.detectChanges();
            }, 100);
          },
          error: (err: any) => {
            console.log(err)
          }
        });
      },
      error: (err: any) => {
        console.log(err)
      }
    });
  }
}

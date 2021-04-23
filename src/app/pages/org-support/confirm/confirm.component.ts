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
import { UserEditResponseInfo } from 'src/app/models/user';
import { Group, GroupList } from 'src/app/models/organisationGroup';
import { WrapperOrganisationGroupService } from 'src/app/services/wrapper/wrapper-org--group-service';

@Component({
  selector: 'app-org-support-confirm',
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
export class OrgSupportConfirmComponent extends BaseComponent implements OnInit {

  public user$!: Observable<any>;
  private user: any;
  public orgGroups!: Group[];
  public roles$!: Observable<any>;
  public roles!: [];

  constructor(private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private organisationGroupService: WrapperOrganisationGroupService, private contactService: contactService, private wrapperOrgService: WrapperOrganisationService, private wrapperUserService: WrapperUserService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params.id) {
        this.user$ = this.wrapperUserService.getUser(params.id).pipe(share());
        this.user$.subscribe({
          next: result => {
            this.user = result;
            this.getOrgGroups();
          }
        });
      }
    });
  }

  public onSubmitClick() {
    this.authService.resetPassword(this.user.userName).subscribe(() => {
      this.setOrgRoles();
      // this.wrapperUserService.updateUser(this.user.userName, this.user).subscribe({
      //   next: (userEditResponseInfo: UserEditResponseInfo) => {
      //     if (userEditResponseInfo.userId == this.user.userName) {
      //       this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
      //     }
      //     else {
      //       console.log("TODO: navigate to error page");
      //       this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
      //     }
      //   },
      //   error: (err: any) => {
      //     console.log(err);
      //     console.log("TODO: tell user");
      //   }
      // });
    });
  }

  setOrgRoles() {
    this.roles$ = this.organisationGroupService.getOrganisationRoles(this.user.organisationId).pipe(share());
    this.roles$.subscribe({
      next: (roles) => {
        const role = roles.find((x: any) => x.roleName === 'Organisation Administrator');
        if (role) {
          if (this.isAssigned()) { // Remove
            this.wrapperUserService.updateUserRoles(localStorage.getItem('user_name')+'', this.user).subscribe({
              next: (userEditResponseInfo: UserEditResponseInfo) => {
                if (userEditResponseInfo.userId == this.user.userName) {
                  this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
                }
                else {
                  console.log("TODO: navigate to error page");
                  this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
                }
              },
              error: (err: any) => {
                console.log(err);
                console.log("TODO: tell user");
              }
            });
          } else { // Add
            this.wrapperUserService.addAdminRole(localStorage.getItem('user_name')+'', this.user).subscribe({
              next: (userEditResponseInfo: UserEditResponseInfo) => {
                if (userEditResponseInfo.userId == this.user.userName) {
                  this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
                }
                else {
                  console.log("TODO: navigate to error page");
                  this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
                }
              },
              error: (err: any) => {
                console.log(err);
                console.log("TODO: tell user");
              }
            });
          }
          
          // if (this.isAssigned()) {
          //   this.user.roleIds.forEach((item: any, index: any) => {
          //     if (item === role['roleId']) this.user.roleIds.splice(index,1);
          //   });
          //   this.user.roleNames.forEach((item: any, index: any) => {
          //     if (item === 'ORG_ADMINISTRATOR') this.user.roleNames.splice(index,1);
          //   });
          // } else {
          //   this.user.roleIds.push(role['roleId']);
          //   this.user.roleNames.push('ORG_ADMINISTRATOR');
          // }
          // this.wrapperUserService.updateUserRoles(localStorage.getItem('user_name')+'', this.user).subscribe({
          //   next: (userEditResponseInfo: UserEditResponseInfo) => {
          //     if (userEditResponseInfo.userId == this.user.userName) {
          //       this.router.navigateByUrl(`org-support/success-changed-role/${this.user.userName}`);
          //     }
          //     else {
          //       console.log("TODO: navigate to error page");
          //       this.router.navigateByUrl(`org-support/success-changed-role/${this.user.userName}`);
          //     }
          //   },
          //   error: (err: any) => {
          //     console.log(err);
          //     console.log("TODO: tell user");
          //   }
          // });
        } else {
          this.router.navigateByUrl(`org-support/success/${this.user.userName}`);
        }
      }
    });
  }

  isAssigned(): boolean {
    const adminName = 'ORG_ADMINISTRATOR';
    if (this.user.roleNames.includes(adminName)) {
      return true;
    }
    else if (this.user.userGroups.some((i: { accessRole: string; }) => i.accessRole === adminName)) {
      return true;
    }
    else {
      return false;
    }
  }

  public onCancelClick() {
    this.router.navigateByUrl('org-support/search');
  }

  getOrgGroups() {
    this.organisationGroupService.getOrganisationGroups(this.user.organisationId).subscribe({
        next: (orgGroups: GroupList) => {
          this.orgGroups = orgGroups.groupList;
          this.orgGroups.map(group => {
              let isGroupOfUser: boolean = false;
              isGroupOfUser = this.user.groupIds && this.user.groupIds.indexOf(group.groupId) != -1 ? true : false;
          });
        },
        error: (err: any) => {
          console.log(err)
        }
    });
  }

  setOrgGroups() {
    let selectedGroupIds: number[] = [];
    this.orgGroups.map(group => {
      if (group.groupName === 'Organisation Administrator') {
        selectedGroupIds.push(group.groupId);
      }
    });
    this.user.groupIds = selectedGroupIds;
  }
}

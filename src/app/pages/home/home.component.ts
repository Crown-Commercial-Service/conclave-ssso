import { ChangeDetectionStrategy, Component, OnInit, Pipe, ViewEncapsulation } from '@angular/core';
import { Store } from '@ngrx/store';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { dataService } from 'src/app/services/data/data.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { UIState } from 'src/app/store/ui.states';
import { SystemModule } from 'src/app/models/system';
import { environment } from 'src/environments/environment';
import { TokenService } from 'src/app/services/auth/token.service';
import { WrapperUserService } from 'src/app/services/wrapper/wrapper-user.service';
import { AuthService } from 'src/app/services/auth/auth.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent extends BaseComponent implements OnInit {

  systemModules: SystemModule[] = [];
  ccsModules: SystemModule[] = [];
  idam_client_id: string = environment.idam_client_id;
  targetURL: string = environment.uri.api.security;
  accesstoken: any;
  opIFrameURL = this.sanitizer.bypassSecurityTrustResourceUrl(environment.uri.api.security + '/security/checksession/?origin=' + environment.uri.web.dashboard);
  rpIFrameURL = this.sanitizer.bypassSecurityTrustResourceUrl(environment.uri.web.dashboard + '/assets/rpIFrame.html');
  
  constructor(protected uiStore: Store<UIState>, private sanitizer: DomSanitizer, private authService: AuthService, private organisationService: OrganisationService, private wrapperUserService: WrapperUserService, private readonly tokenService: TokenService) {
    super(uiStore);
    this.accesstoken = this.tokenService.getDecodedAccessToken();
    console.log(this.accesstoken);
    localStorage.setItem('cii_organisation_id', this.accesstoken.ciiOrgId);
  }

  ngOnInit() {
    this.authService.getPermissions(this.accesstoken).toPromise().then((response) => {
      console.log(response);
      response.forEach((e: any, i: any) => {
        // if (e.roleName === 'CCS Administrator') {
        //   if (this.hasAccess(e.roleName)) {
        //     this.load(e);
        //   }
        // }
        // if (e.roleName === 'Organisation Administrator') {
        //   if (this.hasAccess(e.roleName)) {
        //     this.load(e);
        //   }
        // }
        // if (e.roleName === 'Organisation User') {
        //   if (this.hasAccess(e.roleName)) {
        //     this.load(e);
        //   }
        // }
        if (this.hasAccess(e.roleName)) {
          this.load(e);
        }
      });
      // if (this.hasAccess('Organisation Administrator')) {
      //   this.systemModules.push({ name: 'Manage users', description: 'Create and manage users and what they can do', route: '/manage-users' });
      //   this.systemModules.push({ name: 'Manage organisation(s)', description: 'View details for your organisation', route: '/manage-org/profile' });
      //   this.systemModules.push({ name: 'Manage groups', description: 'Create groups and organise users', route: '/' });
      // }
      // this.systemModules.push({ name: 'Manage my account', description: 'Manage your details and request a new role', route: '/profile' });
      // if (this.hasAccess('CCS Administrator')) {
      //   this.systemModules.push({ name: 'Manage sign in providers', description: 'Add and manage sign in providers', route: '/' });
      // }
      // if (this.hasAccess('Organisation User')) {
      //   this.ccsModules.push({ name: 'DigiTS', description: 'Book rail, accomodation, air travel, and more', route: '/' });
      //   this.ccsModules.push({ name: 'Buy a thing', description: 'Online catalog to purchase low volume, fixed price commodities', route: '/' });
      //   this.ccsModules.push({ name: 'Evidence locker', description: '', route: '/' });
      //   this.ccsModules.push({ name: 'Agreement service', description: '', route: '/' });
      // }
      // this.ccsModules.push({ name: 'Test', description: 'Test app to demonstrate single sign-on', route: '/token' });
    });
  }

  public hasAccess(role: string): boolean {
    if (this.accesstoken) {
      const roles = JSON.parse(this.accesstoken.roles);
      const match = roles.find((x: { AccessRole: string, Group: string }) => x.AccessRole === role);
      if (match) {
        return true;
      }
    }
    return false;
  }

  public load(e:any) {
    if (e.permissionName === 'MANAGE_USERS') {
      this.systemModules.push({ name: 'Manage users', description: 'Create and manage users and what they can do', route: '/manage-users' });
    }
    if (e.permissionName === 'MANAGE_ORG') {
      this.systemModules.push({ name: 'Manage organisation(s)', description: 'View details for your organisation', route: '/manage-org/profile' });
    }
    if (e.permissionName === 'MANAGE_GROUPS') {
      this.systemModules.push({ name: 'Manage groups', description: 'Create groups and organise users', route: '/' });
    }
    if (e.permissionName === 'MANAGE_MY_ACCOUNT') {
      this.systemModules.push({ name: 'Manage my account', description: 'Manage your details and request a new role', route: '/profile' });
    }
    if (e.permissionName === 'MANAGE_SIGN_IN_PROVIDERS') {
      this.systemModules.push({ name: 'Manage sign in providers', description: 'Add and manage sign in providers', route: '/' });
    }
    if (e.permissionName === 'TEST_CLIENT') {
      this.ccsModules.push({ name: 'Test', description: 'Test app to demonstrate single sign-on', route: '/token' });
    }
  }
}

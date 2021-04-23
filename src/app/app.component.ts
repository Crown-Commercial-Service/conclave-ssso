import { Component, HostBinding, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { OverlayContainer } from '@angular/cdk/overlay';
import { TranslateService } from '@ngx-translate/core';
import { UIState } from './store/ui.states';
import { getSideNavVisible } from './store/ui.selectors';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from './services/auth/auth.service';
import { environment } from 'src/environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LoadingIndicatorService } from './services/helper/loading-indicator.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  @HostBinding('class') className = '';
  public sideNavVisible$: Observable<boolean>;
  isAuthenticated: boolean = false;
  toggleControl = new FormControl(false);
  opIFrameURL = this.sanitizer.bypassSecurityTrustResourceUrl(environment.uri.api.security + '/security/checksession/?origin=' + environment.uri.web.dashboard);
  rpIFrameURL = this.sanitizer.bypassSecurityTrustResourceUrl(environment.uri.web.dashboard + '/assets/rpIFrame.html');

  constructor(private sanitizer: DomSanitizer, private overlay: OverlayContainer, private translate: TranslateService, protected uiStore: Store<UIState>, private router: Router,
    private route: ActivatedRoute, public authService: AuthService, public loadingIndicatorService: LoadingIndicatorService ) {
    translate.setDefaultLang('en');
    this.sideNavVisible$ = this.uiStore.pipe(select(getSideNavVisible));
  }

  ngOnInit(): void {
    this.isAuthenticated = this.authService.isUserAuthenticated();
    if (this.isAuthenticated) {
      this.authService.registerTokenRenewal();
    }
    this.toggleControl.valueChanges.subscribe((darkMode) => {
      const darkClassName = 'darkMode';
      this.className = darkMode ? darkClassName : '';
      if (darkMode) {
        this.overlay.getContainerElement().classList.add(darkClassName);
      } else {
        this.overlay.getContainerElement().classList.remove(darkClassName);
      }
    });
    if (!localStorage.getItem('client_id')) {
      localStorage.setItem('client_id', environment.idam_client_id);
    }

    if (!localStorage.getItem('securityapiurl')) {
      localStorage.setItem('securityapiurl', environment.uri.api.security);
    }

    if (!localStorage.getItem('redirect_uri')) {
      localStorage.setItem('redirect_uri', environment.uri.web.dashboard);
    }
  }

  navigate(tab: any, subLink = null) {
    // this.selectedTab = tab.name;
  }

  onToggle(): void {
    this.uiStore.dispatch({ type: '[UI] Side Nav Toggle' });
  }

  public logOut(): void {
    this.authService.logOutAndRedirect();
  }
  public signout() {
    this.authService.logOutAndRedirect();
  }

  // public isAuthenticated(): Observable<boolean> {
  //   return this.authService.isAuthenticated();
  // }
}

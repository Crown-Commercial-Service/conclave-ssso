import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HttpClient, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableModule } from '@angular/material/table';
import { FlexLayoutModule } from '@angular/flex-layout';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzSliderModule } from 'ng-zorro-antd/slider';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { StoreModule } from '@ngrx/store';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import * as reducers from './store/ui.reducers';
import { HttpJwtAuthInterceptor } from './interceptors/http-jtw-auth.interceptor';
import { HttpBasicAuthInterceptor } from './interceptors/http-basic-auth.interceptor';
import { AuthService } from './services/auth/auth.service';
import { TokenService } from './services/auth/token.service';
import { ComponentsModule } from './components/index';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { ChangePasswordComponent } from './pages/change-password/change-password.component';
import { TokenComponent } from './pages/token/token.component';
import { AuthSuccessComponent } from './pages/auth-success/auth-success.component';
import { RegistrationSuccessComponent } from './pages/registration-success/registration-success.component';
import { ManageOrganisationLoginComponent } from './pages/manage-organisation/manage-organisation-login/manage-organisation-login.component';
import { ManageOrgRegStep1Component } from './pages/manage-organisation/manage-organisation-registration-step-1/manage-organisation-registration-step-1.component';
import { ManageOrgRegStep2Component } from './pages/manage-organisation/manage-organisation-registration-step-2/manage-organisation-registration-step-2.component';
import { ManageOrgRegStep3Component } from './pages/manage-organisation/manage-organisation-registration-step-3/manage-organisation-registration-step-3.component';
import { ManageOrgRegAdditionalIdentifiersComponent } from './pages/manage-organisation/manage-organisation-registration-additional-identifiers/manage-organisation-registration-additional-identifiers.component';
import { ManageOrgRegAddUserComponent } from './pages/manage-organisation/manage-organisation-registration-add-user/manage-organisation-registration-add-user.component';
import { ManageOrgRegSuccessComponent } from './pages/manage-organisation/manage-organisation-registration-success/manage-organisation-registration-success.component';
import { ManageOrgRegErrorComponent } from './pages/manage-organisation/manage-organisation-registration-error/manage-organisation-registration-error.component';
import { ManageOrgRegErrorUsernameExistsComponent } from './pages/manage-organisation/manage-organisation-registration-error-username-already-exists/manage-organisation-registration-error-username-already-exists.component';
import { ManageOrgRegErrorNotFoundComponent } from './pages/manage-organisation/manage-organisation-registration-error-not-found/manage-organisation-registration-error-not-found.component';
import { ManageOrgRegFailureComponent } from './pages/manage-organisation/manage-organisation-registration-failure/manage-organisation-registration-failure.component';
import { ManageOrgRegConfirmComponent } from './pages/manage-organisation/manage-organisation-registration-confirm/manage-organisation-registration-confirm.component';
import { ManageOrgRegDetailsWrongComponent } from './pages/manage-organisation/manage-organisation-registration-error-details-wrong/manage-organisation-registration-error-details-wrong.component';
import { ManageOrgRegOrgNotFoundComponent } from './pages/manage-organisation/manage-organisation-registration-error-not-my-organisation/manage-organisation-registration-error-not-my-organisation.component';
import { ManageOrganisationContactEditComponent } from './pages/manage-organisation/manage-organisation-contact-edit/manage-organisation-contact-edit.component';
import { ManageOrganisationContactDeleteComponent } from './pages/manage-organisation/manage-organisation-contact-delete/manage-organisation-contact-delete.component';
import { ManageOrganisationContactOperationSuccessComponent } from './pages/manage-organisation/manage-organisation-contact-operation-success/manage-organisation-contact-operation-success.component';
import { ManageOrganisationProfileComponent } from './pages/manage-organisation/manage-organisation-profile/manage-organisation-profile.component';
import { UserProfileComponent } from './pages/user-profile/user-profile-component';
import { UserContactEditComponent } from './pages/user-contact/user-contact-edit/user-contact-edit.component';
import { OperationSuccessComponent } from './pages/operation-success/operation-success.component';
import { OperationFailedComponent } from './pages/operation-failed/operation-failed.component';
import { ManageOrganisationRegistryComponent } from './pages/manage-organisation/manage-organisation-profile-registry/manage-organisation-profile-registry.component';
import { ManageOrganisationRegistrySearchComponent } from './pages/manage-organisation/manage-organisation-profile-registry-search/manage-organisation-profile-registry-search.component';
import { ManageOrganisationRegistryConfirmComponent } from './pages/manage-organisation/manage-organisation-profile-registry-confirm/manage-organisation-profile-registry-confirm.component';
import { ManageOrganisationRegistryDetailsWrongComponent } from './pages/manage-organisation/manage-organisation-profile-registry-error-details-wrong/manage-organisation-profile-registry-error-details-wrong.component';
import { ManageOrganisationRegistryOrgNotFoundComponent } from './pages/manage-organisation/manage-organisation-profile-registry-error-not-my-organisation/manage-organisation-profile-registry-error-not-my-organisation.component';
import { ManageOrganisationRegistryConfirmAdditionalDetailsComponent } from './pages/manage-organisation/manage-organisation-profile-registry-confirm-additional-identifiers/manage-organisation-profile-registry-confirm-additional-identifiers.component';
import { ManageOrganisationRegistryDeleteComponent } from './pages/manage-organisation/manage-organisation-profile-registry-delete/manage-organisation-profile-registry-delete.component';
import { ManageOrganisationRegistryDeleteConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-registry-delete-confirm/manage-organisation-profile-registry-delete-confirm.component';
import { ManageOrganisationRegistryAddConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-registry-add-confirmed/manage-organisation-profile-registry-add-confirmed.component';
import { GovUKTableComponent } from './components/govuk-table/govuk-table.component';
import { ManageOrganisationRegistryErrorComponent } from './pages/manage-organisation/manage-organisation-profile-registry-error/manage-organisation-profile-registry-error.component';
import { ErrorComponent } from './pages/error/error.component';
import { ManageUserProfilesComponent } from './pages/manage-user/manage-user-profiles/manage-user-profiles-component';
import { ManageUserAddSelectionComponent } from './pages/manage-user/manage-user-add-selection/manage-user-add-selection-component';
import { ManageUserAddSingleUserDetailComponent } from './pages/manage-user/manage-user-add-single-user-detail/manage-user-add-single-user-detail.component';
import { ManageUserConfirmResetPasswordComponent } from './pages/manage-user/manage-user-confirm-reset-password/manage-user-confirm-reset-password-component';
import { EnumToArrayPipe } from './pipes/enumToArrayPipe';
import { ManageOrganisationSiteEditConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-edit-confirm/manage-organisation-profile-site-edit-confirm.component';
import { ManageOrganisationSiteEditComponent } from './pages/manage-organisation/manage-organisation-profile-site-edit/manage-organisation-profile-site-edit.component';
import { ManageOrganisationSiteDeleteConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-delete-confirm/manage-organisation-profile-site-delete-confirm.component';
import { ManageOrganisationSiteDeleteComponent } from './pages/manage-organisation/manage-organisation-profile-site-delete/manage-organisation-profile-site-delete.component';
import { ManageOrganisationSiteAddConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-add-confirm/manage-organisation-profile-site-add-confirm.component';
import { ManageOrganisationSiteAddComponent } from './pages/manage-organisation/manage-organisation-profile-site-add/manage-organisation-profile-site-add.component';
import { UserContactDeleteConfirmComponent } from './pages/user-contact/user-contact-delete-confirm/user-contact-delete-confirm-component';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http);
}

export function createTranslateLoader(http: HttpClient) {
  return new TranslateHttpLoader(http, '../assets/i18n/', '.json');
}

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    LoginComponent,
    OperationSuccessComponent,
    OperationFailedComponent,
    RegisterComponent,
    UserProfileComponent,
    UserContactEditComponent,
    UserContactDeleteConfirmComponent,
    ForgotPasswordComponent,
    ChangePasswordComponent,
    TokenComponent,
    AuthSuccessComponent,
    RegistrationSuccessComponent,
    ManageOrganisationLoginComponent,
    ManageOrganisationProfileComponent,
    ManageOrgRegStep1Component,
    ManageOrgRegStep2Component,
    ManageOrgRegStep3Component,
    ManageOrgRegAdditionalIdentifiersComponent,
    ManageOrgRegAddUserComponent,
    ManageOrgRegConfirmComponent,
    ManageOrgRegSuccessComponent,
    ManageOrgRegErrorComponent,
    ManageOrgRegErrorUsernameExistsComponent,
    ManageOrgRegErrorNotFoundComponent,
    ManageOrgRegFailureComponent,
    ManageOrgRegDetailsWrongComponent,
    ManageOrgRegOrgNotFoundComponent,
    ManageOrganisationContactEditComponent,
    ManageOrganisationContactDeleteComponent,
    ManageOrganisationContactOperationSuccessComponent,
    ManageOrganisationRegistryComponent,
    ManageOrganisationRegistrySearchComponent,
    ManageOrganisationRegistryConfirmComponent,
    ManageOrganisationRegistryConfirmAdditionalDetailsComponent,
    ManageOrganisationRegistryAddConfirmationComponent,
    ManageOrganisationRegistryDetailsWrongComponent,
    ManageOrganisationRegistryOrgNotFoundComponent,
    ManageOrganisationRegistryDeleteComponent,
    ManageOrganisationRegistryDeleteConfirmationComponent,
    ManageOrganisationRegistryErrorComponent,
    ErrorComponent,
    GovUKTableComponent,
    ManageUserProfilesComponent,
    ManageUserAddSelectionComponent,
    ManageUserAddSingleUserDetailComponent,
    ManageUserConfirmResetPasswordComponent,
    EnumToArrayPipe,
    ManageOrganisationSiteAddComponent,
    ManageOrganisationSiteAddConfirmationComponent,
    ManageOrganisationSiteDeleteComponent,
    ManageOrganisationSiteDeleteConfirmationComponent,
    ManageOrganisationSiteEditComponent,
    ManageOrganisationSiteEditConfirmationComponent
  ],
  imports: [
    // BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: (createTranslateLoader),
        deps: [HttpClient]
      }
    }),
    StoreModule.forRoot({}),
    StoreModule.forFeature('ui-state', reducers.reducer),
    FlexLayoutModule,
    MatToolbarModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatExpansionModule,
    NzButtonModule,
    NzDropDownModule,
    NzMenuModule,
    NzBreadCrumbModule,
    NzSliderModule,
    NzSwitchModule,
    NzLayoutModule,
    ComponentsModule,
    MatTableModule,
  ],
  exports: [TranslateModule],
  providers: [
    AuthService,
    TokenService,
    { provide: HTTP_INTERCEPTORS, useClass: HttpJwtAuthInterceptor, multi: true },
    // { provide: HTTP_INTERCEPTORS, useClass: HttpBasicAuthInterceptor, multi: true },
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

import { NgModule } from '@angular/core';
import { Routes, RouterModule, ExtraOptions } from '@angular/router';

import { AuthGuard } from './services/auth/auth.guard';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { TokenComponent } from './pages/token/token.component';
import { ChangePasswordComponent } from './pages/change-password/change-password.component';
import { AuthSuccessComponent } from './pages/auth-success/auth-success.component';
import { RegistrationSuccessComponent } from './pages/registration-success/registration-success.component';
import { ManageOrganisationLoginComponent } from './pages/manage-organisation/manage-organisation-login/manage-organisation-login.component';
import { ManageOrgRegStep1Component } from './pages/manage-organisation/manage-organisation-registration-step-1/manage-organisation-registration-step-1.component';
import { ManageOrgRegStep2Component } from './pages/manage-organisation/manage-organisation-registration-step-2/manage-organisation-registration-step-2.component';
import { ManageOrgRegStep3Component } from './pages/manage-organisation/manage-organisation-registration-step-3/manage-organisation-registration-step-3.component';
import { ManageOrgRegAdditionalIdentifiersComponent } from './pages/manage-organisation/manage-organisation-registration-additional-identifiers/manage-organisation-registration-additional-identifiers.component';
import { ManageOrgRegAddUserComponent } from './pages/manage-organisation/manage-organisation-registration-add-user/manage-organisation-registration-add-user.component';
import { ManageOrgRegSuccessComponent } from './pages/manage-organisation/manage-organisation-registration-success/manage-organisation-registration-success.component';
import { ManageOrgRegFailureComponent } from './pages/manage-organisation/manage-organisation-registration-failure/manage-organisation-registration-failure.component';
import { ManageOrgRegErrorComponent } from './pages/manage-organisation/manage-organisation-registration-error/manage-organisation-registration-error.component';
import { ManageOrgRegErrorUsernameExistsComponent } from './pages/manage-organisation/manage-organisation-registration-error-username-already-exists/manage-organisation-registration-error-username-already-exists.component';
import { ManageOrgRegErrorNotFoundComponent } from './pages/manage-organisation/manage-organisation-registration-error-not-found/manage-organisation-registration-error-not-found.component';
import { ManageOrgRegConfirmComponent } from './pages/manage-organisation/manage-organisation-registration-confirm/manage-organisation-registration-confirm.component';
import { ManageOrgRegDetailsWrongComponent } from './pages/manage-organisation/manage-organisation-registration-error-details-wrong/manage-organisation-registration-error-details-wrong.component';
import { ManageOrgRegOrgNotFoundComponent } from './pages/manage-organisation/manage-organisation-registration-error-not-my-organisation/manage-organisation-registration-error-not-my-organisation.component';
import { ManageOrganisationProfileComponent } from './pages/manage-organisation/manage-organisation-profile/manage-organisation-profile.component';
import { ManageOrganisationContactEditComponent } from './pages/manage-organisation/manage-organisation-contact-edit/manage-organisation-contact-edit.component';
import { ManageOrganisationContactDeleteComponent } from './pages/manage-organisation/manage-organisation-contact-delete/manage-organisation-contact-delete.component';
import { ManageOrganisationContactOperationSuccessComponent } from './pages/manage-organisation/manage-organisation-contact-operation-success/manage-organisation-contact-operation-success.component';
import { UserProfileComponent } from './pages/user-profile/user-profile-component';
import { ManageOrgRegErrorGenericComponent } from './pages/manage-organisation/manage-organisation-registration-error-generic/manage-organisation-registration-error-generic.component';
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
import { ManageOrganisationRegistryErrorComponent } from './pages/manage-organisation/manage-organisation-profile-registry-error/manage-organisation-profile-registry-error.component';
import { ErrorComponent } from './pages/error/error.component';
import { ManageUserProfilesComponent } from './pages/manage-user/manage-user-profiles/manage-user-profiles-component';
import { ManageUserAddSelectionComponent } from './pages/manage-user/manage-user-add-selection/manage-user-add-selection-component';
import { ManageUserAddSingleUserDetailComponent } from './pages/manage-user/manage-user-add-single-user-detail/manage-user-add-single-user-detail.component';
import { ManageUserConfirmResetPasswordComponent } from './pages/manage-user/manage-user-confirm-reset-password/manage-user-confirm-reset-password-component';
import { ManageOrganisationSiteAddComponent } from './pages/manage-organisation/manage-organisation-profile-site-add/manage-organisation-profile-site-add.component';
import { ManageOrganisationSiteAddConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-add-confirm/manage-organisation-profile-site-add-confirm.component';
import { ManageOrganisationSiteDeleteComponent } from './pages/manage-organisation/manage-organisation-profile-site-delete/manage-organisation-profile-site-delete.component';
import { ManageOrganisationSiteDeleteConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-delete-confirm/manage-organisation-profile-site-delete-confirm.component';
import { ManageOrganisationSiteEditComponent } from './pages/manage-organisation/manage-organisation-profile-site-edit/manage-organisation-profile-site-edit.component';
import { ManageOrganisationSiteEditConfirmationComponent } from './pages/manage-organisation/manage-organisation-profile-site-edit-confirm/manage-organisation-profile-site-edit-confirm.component';
import { UserContactDeleteConfirmComponent } from './pages/user-contact/user-contact-delete-confirm/user-contact-delete-confirm-component';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', canActivate: [AuthGuard], pathMatch: 'full', component: HomeComponent },
  { path: 'error', pathMatch: 'full', component: ErrorComponent },
  { path: 'profile', canActivate: [AuthGuard], pathMatch: 'full', component: UserProfileComponent },
  { path: 'operation-success/:operation', canActivate: [AuthGuard], pathMatch: 'full', component: OperationSuccessComponent },
  { path: 'operation-failed/:operation', pathMatch: 'full', component: OperationFailedComponent },
  { path: 'change-password-success/:operation', pathMatch: 'full', component: OperationSuccessComponent },
  { path: 'change-password-failed/:operation', pathMatch: 'full', component: OperationFailedComponent },
  { path: 'user-contact-edit', canActivate: [AuthGuard], pathMatch: 'full', component: UserContactEditComponent },
  { path: 'user-contact-delete', canActivate: [AuthGuard], pathMatch: 'full', component: UserContactDeleteConfirmComponent },
  { path: 'register', pathMatch: 'full', component: RegisterComponent },
  { path: 'forgot-password', pathMatch: 'full', component: ForgotPasswordComponent },
  { path: 'change-password', canActivate: [AuthGuard], pathMatch: 'full', component: ChangePasswordComponent },
  { path: 'token', canActivate: [AuthGuard], pathMatch: 'full', component: TokenComponent },
  { path: 'authsuccess', component: AuthSuccessComponent },
  { path: 'registration/success', component: RegistrationSuccessComponent },
  { path: 'manage-org/login', pathMatch: 'full', component: ManageOrganisationLoginComponent },
  { path: 'manage-org/register', pathMatch: 'full', component: ManageOrgRegStep1Component },
  { path: 'manage-org/register/search', pathMatch: 'full', component: ManageOrgRegStep2Component },
  { path: 'manage-org/register/search/:scheme/:id', pathMatch: 'full', component: ManageOrgRegStep3Component },
  { path: 'manage-org/register/search/:scheme/:id/additional-identifiers', pathMatch: 'full', component: ManageOrgRegAdditionalIdentifiersComponent },
  { path: 'manage-org/register/user', pathMatch: 'full', component: ManageOrgRegAddUserComponent },
  { path: 'manage-org/register/confirm', pathMatch: 'full', component: ManageOrgRegConfirmComponent },
  { path: 'manage-org/register/success', pathMatch: 'full', component: ManageOrgRegSuccessComponent },
  { path: 'manage-org/register/error', pathMatch: 'full', component: ManageOrgRegErrorComponent },
  { path: 'manage-org/register/error/generic', pathMatch: 'full', component: ManageOrgRegErrorGenericComponent },
  { path: 'manage-org/register/error/username', pathMatch: 'full', component: ManageOrgRegErrorUsernameExistsComponent },
  { path: 'manage-org/register/error/notfound', pathMatch: 'full', component: ManageOrgRegErrorNotFoundComponent },
  { path: 'manage-org/register/error/reg-id-exists', pathMatch: 'full', component: ManageOrgRegFailureComponent },
  { path: 'manage-org/register/error/wrong-details', pathMatch: 'full', component: ManageOrgRegDetailsWrongComponent },
  { path: 'manage-org/register/error/not-my-details', pathMatch: 'full', component: ManageOrgRegOrgNotFoundComponent },
  { path: 'manage-org/profile', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationProfileComponent },
  { path: 'manage-org/profile/:organisationId/contact-edit/:contactId', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationContactEditComponent },
  { path: 'manage-org/profile/:organisationId/contact-delete/:contactId', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationContactDeleteComponent },
  { path: 'manage-org/profile/:organisationId/contact-operation-success/:operation', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationContactOperationSuccessComponent },
  { path: 'manage-org/profile/:organisationId/registry', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryComponent },
  { path: 'manage-org/profile/:organisationId/registry/error/:reason', canActivate: [AuthGuard], component: ManageOrganisationRegistryErrorComponent },
  // { path: 'manage-org/profile/:organisationId/registry/:scheme/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryComponent },
  { path: 'manage-org/profile/registry/search', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistrySearchComponent },
  { path: 'manage-org/profile/:organisationId/registry/search', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistrySearchComponent },
  { path: 'manage-org/profile/:organisationId/registry/search/:scheme/:id/additional-identifiers', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryConfirmAdditionalDetailsComponent },
  { path: 'manage-org/profile/:organisationId/registry/search/:scheme/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryConfirmComponent },
  { path: 'manage-org/profile/:organisationId/registry/confirmation/:scheme/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryAddConfirmationComponent },
  { path: 'manage-org/profile/:organisationId/registry/search/wrong-details', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryDetailsWrongComponent },
  { path: 'manage-org/profile/:organisationId/registry/search/not-my-org', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryOrgNotFoundComponent },
  { path: 'manage-org/profile/:organisationId/registry/delete/:scheme/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryDeleteComponent },
  { path: 'manage-org/profile/:organisationId/registry/delete/confirmation/:scheme/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationRegistryDeleteConfirmationComponent },
  { path: 'manage-users', pathMatch: 'full', canActivate: [AuthGuard], component: ManageUserProfilesComponent },
  { path: 'manage-users/add-user-selection', pathMatch: 'full', canActivate: [AuthGuard], component: ManageUserAddSelectionComponent },
  { path: 'manage-users/add-user/details', pathMatch: 'full', canActivate: [AuthGuard], component: ManageUserAddSingleUserDetailComponent },
  { path: 'manage-users/confirm-reset-password', pathMatch: 'full', canActivate: [AuthGuard], component: ManageUserConfirmResetPasswordComponent },
  { path: 'manage-org/profile/site/add', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteAddComponent },
  { path: 'manage-org/profile/site/add/success', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteAddConfirmationComponent },
  { path: 'manage-org/profile/site/edit/success', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteEditConfirmationComponent },
  { path: 'manage-org/profile/site/edit/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteEditComponent },
  { path: 'manage-org/profile/site/delete/success', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteDeleteConfirmationComponent },
  { path: 'manage-org/profile/site/delete/:id', pathMatch: 'full', canActivate: [AuthGuard], component: ManageOrganisationSiteDeleteComponent },
  { path: '**', redirectTo: 'home', pathMatch: 'full' }
];

export const routingConfiguration: ExtraOptions = {
  paramsInheritanceStrategy: 'always'
};


@NgModule({
  imports: [RouterModule.forRoot(routes, routingConfiguration)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

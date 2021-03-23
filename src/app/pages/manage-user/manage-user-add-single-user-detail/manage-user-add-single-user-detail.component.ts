import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormArray } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { KeyValue, Location, ViewportScroller } from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum, UserTitleEnum } from 'src/app/constants/enum';
import { ContactInfo } from 'src/app/models/userContact';
import { WrapperUserContactService } from 'src/app/services/wrapper/wrapper-user-contact.service';
import { ScrollHelper } from 'src/app/services/helper/scroll-helper.services';
import { ContactReason } from 'src/app/models/contactDetail';
import { WrapperConfigurationService } from 'src/app/services/wrapper/wrapper-configuration.service';
import { UserProfileRequestInfo, UserProfileResponseInfo } from 'src/app/models/user';
import { WrapperOrganisationGroupService } from 'src/app/services/wrapper/wrapper-org--group-service';
import { Group } from 'src/app/models/organisationGroup';
import { IdentityProvider } from 'src/app/models/identityProvider';
import { environment } from 'src/environments/environment';
import { WrapperUserService } from 'src/app/services/wrapper/wrapper-user.service';
import { AuthService } from 'src/app/services/auth/auth.service';

@Component({
    selector: 'app-manage-user-add-single-user-detail',
    templateUrl: './manage-user-add-single-user-detail.component.html',
    styleUrls: ['./manage-user-add-single-user-detail.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageUserAddSingleUserDetailComponent extends BaseComponent implements OnInit {

    organisationId: string;
    userProfileRequestInfo: UserProfileRequestInfo;
    userProfileResponseInfo: UserProfileResponseInfo;
    userProfileForm: FormGroup;
    submitted!: boolean;
    orgGroups: Group[];
    identityProviders: IdentityProvider[];
    userNamePasswordIentityProviderConnectionName: string = environment.userNamePasswordIdentityProviderConnectionName;
    isEdit: boolean = false;
    editingUserName: string = "";
    userTitleEnum = UserTitleEnum;

    constructor(private organisationGroupService: WrapperOrganisationGroupService, private formBuilder: FormBuilder, private router: Router,
        private location: Location, private activatedRoute: ActivatedRoute, protected uiStore: Store<UIState>,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private configurationService: WrapperConfigurationService,
        private wrapperUserService: WrapperUserService, private authService: AuthService) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            console.log(routeData);
            this.isEdit = routeData['isEdit'];
            this.editingUserName = routeData['userName'];
        }
        this.orgGroups = [];
        this.identityProviders = [];
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.userProfileRequestInfo = {
            organisationId: this.organisationId,
            title:  0,
            userName: '',
            firstName: '',
            lastName: '',
            groupIds: []
        };
        this.userProfileResponseInfo = {
            id: 0,
            organisationId: this.organisationId,
            title: 0,
            canChangePassword: false,
            userName: '',
            firstName: '',
            lastName: '',
            groupIds: [],
            identityProviderId: 0
        };
        this.userProfileForm = this.formBuilder.group({
            userTitle: ['', Validators.compose([Validators.required])],
            firstName: ['', Validators.compose([Validators.required])],
            lastName: ['', Validators.compose([Validators.required])],
            userName: ['', Validators.compose([Validators.required, Validators.email])],
            signInProviderControl: ['', Validators.compose([Validators.required])]
        });
    }

    ngOnInit() {        
        if (this.isEdit) {
            this.wrapperUserService.getUser(this.editingUserName).subscribe({
                next: (userProfileResponseInfo: UserProfileResponseInfo) => {
                    this.userProfileResponseInfo = userProfileResponseInfo;
                    console.log(userProfileResponseInfo);
                    this.userProfileForm.controls['userTitle'].setValue(this.userProfileResponseInfo.title);
                    this.userProfileForm.controls['firstName'].setValue(this.userProfileResponseInfo.firstName);
                    this.userProfileForm.controls['lastName'].setValue(this.userProfileResponseInfo.lastName);
                    this.userProfileForm.controls['userName'].setValue(this.userProfileResponseInfo.userName);
                    this.getOrgGroups(true);
                    this.getIdentityProviders(true);
                },
                error: (err: any) => {
                    console.log(err);
                }
            });
        }
        else {
            this.getOrgGroups();
            this.getIdentityProviders();
        }
    }

    getIdentityProviders(isEdit: boolean = false) {
        this.configurationService.getIdentityProviders().subscribe({
            next: (orgGroups: IdentityProvider[]) => {
                this.identityProviders = orgGroups;
                if (isEdit) {
                    this.userProfileForm.controls['signInProviderControl'].setValue(this.userProfileResponseInfo.identityProviderId || '');
                }
            },
            error: (err: any) => {
                console.log(err)
            }
        });
    }

    getOrgGroups(isEdit: boolean = false) {
        this.organisationGroupService.getOrganisationGroups(this.organisationId).subscribe({
            next: (orgGroups: Group[]) => {
                this.orgGroups = orgGroups;
                this.orgGroups.map(group => {
                    let isGroupOfUser: boolean = false;
                    if (isEdit) {
                        isGroupOfUser = this.userProfileResponseInfo.groupIds &&
                            this.userProfileResponseInfo.groupIds.indexOf(group.groupId) != -1 ? true : false;
                    }
                    this.userProfileForm.addControl('orgGroupControl_' + group.groupId, this.formBuilder.control(isGroupOfUser ? true : ''));
                });
            },
            error: (err: any) => {
                console.log(err)
            }
        });
    }

    ngAfterViewChecked() {
        this.scrollHelper.doScroll();
    }

    scrollToAnchor(elementId: string): void {
        this.viewportScroller.scrollToAnchor(elementId);
    }

    public onSubmit(form: FormGroup) {
        
        this.submitted = true;
        if (this.formValid(form)) {
            this.submitted = false;

            this.userProfileRequestInfo.title = form.get('userTitle')?.value;
            this.userProfileRequestInfo.firstName = form.get('firstName')?.value;
            this.userProfileRequestInfo.lastName = form.get('lastName')?.value;
            this.userProfileRequestInfo.userName = form.get('userName')?.value;
            let identityProviderId = form.get('signInProviderControl')?.value || 0;
            this.userProfileRequestInfo.identityProviderId = identityProviderId;
            let selectedGroupIds: number[] = [];
            this.orgGroups.map(group => {
                if (form.get('orgGroupControl_' + group.groupId)?.value === true) {
                    selectedGroupIds.push(group.groupId);
                }
            });
            this.userProfileRequestInfo.groupIds = selectedGroupIds;

            let registerAuth0 = this.identityProviders.find(i => i.id == this.userProfileRequestInfo.identityProviderId)?.connectionName
                == this.userNamePasswordIentityProviderConnectionName;

            if (this.isEdit) {
                this.updateUser();
            }
            else {
                this.createUserInConclave(registerAuth0, form);
            }

        }
        else {
            this.scrollHelper.scrollToFirst('error-summary');
        }
    }

    updateUser() {
        this.wrapperUserService.updateUser(this.userProfileRequestInfo.userName, this.userProfileRequestInfo).subscribe({
            next: (result: boolean) => {
                if (result) {
                    this.router.navigateByUrl(`operation-success/${OperationEnum.UserUpdate}`);
                }
                else {
                    console.log("Update not success");
                }

            },
            error: (err: any) => {
                console.log("Update Error");
                console.log(err);
            }
        });
    }

    createUserInConclave(registerAuth0: boolean, form: FormGroup) {
        this.wrapperUserService.createUser(this.userProfileRequestInfo).subscribe({
            next: (createdUserEmail: string) => {
                if (createdUserEmail == this.userProfileRequestInfo.userName) {
                    if (registerAuth0) {
                        this.registerInAuth0(form);
                    }
                    else {
                        let data = {
                            'userName': this.userProfileRequestInfo.userName
                        };
                        this.router.navigateByUrl(`operation-success/${OperationEnum.UserCreate}?data=` + JSON.stringify(data));
                    }
                }
                else {
                    console.log("Error in creating");
                }
            },
            error: (err: any) => {
                if (err.status == 409) {
                    form.controls['userName'].setErrors({ 'alreadyExists': true });
                    this.scrollHelper.scrollToFirst('error-summary');
                }
                console.log(err);
            }
        });
    }

    registerInAuth0(form: FormGroup) {
        this.authService.register(this.userProfileRequestInfo.firstName, this.userProfileRequestInfo.lastName,
            this.userProfileRequestInfo.userName, this.userProfileRequestInfo.userName).subscribe({
                next: () => {
                    let data = {
                        'userName': this.userProfileRequestInfo.userName
                    };
                    this.router.navigateByUrl(`operation-success/${OperationEnum.UserCreateWithIdamRegister}?data=` + JSON.stringify(data));
                },
                error: (err: any) => {
                    if (err.error == "USERNAME_EXISTS") {
                        form.controls['userName'].setErrors({ 'alreadyExists': true });
                        this.scrollHelper.scrollToFirst('error-summary');
                    }
                    console.log(err);
                }
            });
    }

    formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        if (this.orgGroups != null && this.orgGroups != undefined && this.orgGroups != []) {
            let selectedGroupIds: number[] = [];
            this.orgGroups.map(group => {
                if (form.get('orgGroupControl_' + group.groupId)?.value === true) {
                    selectedGroupIds.push(group.groupId);
                }
            });
            if (selectedGroupIds.length <= 0) {
                form.setErrors({ groupRequired: true });
                this.scrollHelper.scrollToFirst('error-summary');
                return false;
            }
            else {
                form.setErrors(null);
            }
        }
        return form.valid;
    }

    onCancelClick() {
        this.location.back();
    }

    onResetPasswordClick() {
        let data = {
            'userName': this.editingUserName
        };
        this.router.navigateByUrl('manage-users/confirm-reset-password?data=' + JSON.stringify(data)); 
    }
}

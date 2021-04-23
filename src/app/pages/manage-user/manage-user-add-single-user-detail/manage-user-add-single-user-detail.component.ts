import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormArray } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { KeyValue, Location, LocationStrategy, ViewportScroller } from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum, UserTitleEnum } from 'src/app/constants/enum';
import { ScrollHelper } from 'src/app/services/helper/scroll-helper.services';
import { WrapperConfigurationService } from 'src/app/services/wrapper/wrapper-configuration.service';
import { UserEditResponseInfo, UserProfileRequestInfo, UserProfileResponseInfo } from 'src/app/models/user';
import { WrapperOrganisationGroupService } from 'src/app/services/wrapper/wrapper-org--group-service';
import { Group, GroupList, Role } from 'src/app/models/organisationGroup';
import { IdentityProvider } from 'src/app/models/identityProvider';
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
    orgRoles: Role[];
    identityProviders: IdentityProvider[];
    isEdit: boolean = false;
    editingUserName: string = "";
    userTitleEnum = UserTitleEnum;
    errorLinkClicked: boolean = false;
    routeData: any = {};
    state: any;

    @ViewChildren('input') inputs!: QueryList<ElementRef>;

    constructor(private organisationGroupService: WrapperOrganisationGroupService, private formBuilder: FormBuilder, private router: Router,
        private location: Location, private activatedRoute: ActivatedRoute, protected uiStore: Store<UIState>,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private configurationService: WrapperConfigurationService,
        private wrapperUserService: WrapperUserService, private authService: AuthService, private locationStrategy: LocationStrategy) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        this.state = this.router.getCurrentNavigation()?.extras.state;
        this.locationStrategy.onPopState(() => {
            this.onCancelClick();
        });
        if (queryParams.data) {
            this.routeData = JSON.parse(queryParams.data);
            this.isEdit = this.routeData['isEdit'];
            this.editingUserName = this.routeData['userName'];
        }
        this.orgGroups = [];
        this.orgRoles = [];
        this.identityProviders = [];
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.userProfileRequestInfo = {
            organisationId: this.organisationId,
            title: 0,
            userName: '',
            detail: {
                id: 0,
                groupIds: [],
                roleIds: []
            },
            firstName: '',
            lastName: '',
            
        };
        this.userProfileResponseInfo = {
            userName: '',
            detail: {
                id: 0,
                canChangePassword: false,
                groupIds: [],
                identityProviderId: 0
            },
            organisationId: this.organisationId,
            title: 0,
            firstName: '',
            lastName: '',            
        };
        this.userProfileForm = this.formBuilder.group({
            userTitle: ['', Validators.compose([Validators.required])],
            firstName: ['', Validators.compose([Validators.required])],
            lastName: ['', Validators.compose([Validators.required])],
            userName: ['', Validators.compose([Validators.required, Validators.email])],
            signInProviderControl: ['', Validators.compose([Validators.required])]
        });
        this.viewportScroller.setOffset([0, 100]);
    }

    ngOnInit() {
        if (this.isEdit) {
            this.wrapperUserService.getUser(this.editingUserName).subscribe({
                next: (userProfileResponseInfo: UserProfileResponseInfo) => {
                    if (this.state) {
                        this.userProfileResponseInfo = this.state;
                    }
                    else {
                        this.userProfileResponseInfo = userProfileResponseInfo;
                    }
                    this.userProfileForm.controls['userTitle'].setValue(this.userProfileResponseInfo.title);
                    this.userProfileForm.controls['firstName'].setValue(this.userProfileResponseInfo.firstName);
                    this.userProfileForm.controls['lastName'].setValue(this.userProfileResponseInfo.lastName);
                    this.userProfileForm.controls['userName'].setValue(this.userProfileResponseInfo.userName);
                    this.getOrgGroups();
                    this.getOrgRoles();
                    this.getIdentityProviders();
                },
                error: (err: any) => {
                    console.log(err);
                }
            });
        }
        else {
            if (this.state) {
                this.userProfileResponseInfo = this.state;
                this.userProfileForm.controls['userTitle'].setValue(this.userProfileResponseInfo.title);
                this.userProfileForm.controls['firstName'].setValue(this.userProfileResponseInfo.firstName);
                this.userProfileForm.controls['lastName'].setValue(this.userProfileResponseInfo.lastName);
                this.userProfileForm.controls['userName'].setValue(this.userProfileResponseInfo.userName);
            }
            this.getOrgGroups();
            this.getOrgRoles();
            this.getIdentityProviders();
        }
    }

    getIdentityProviders() {
        this.organisationGroupService.getOrganisationIdentityProviders(this.organisationId).subscribe({
            next: (identityProviders: IdentityProvider[]) => {
                this.identityProviders = identityProviders;
                this.userProfileForm.controls['signInProviderControl'].setValue(this.userProfileResponseInfo.detail.identityProviderId || '');
            },
            error: (err: any) => {
                console.log(err)
            }
        });
    }

    getOrgGroups() {
        this.organisationGroupService.getOrganisationGroups(this.organisationId).subscribe({
            next: (orgGroups: GroupList) => {
                this.orgGroups = orgGroups.groupList;
                this.orgGroups.map(group => {
                    let isGroupOfUser = this.userProfileResponseInfo.detail.groupIds &&
                        this.userProfileResponseInfo.detail.groupIds.indexOf(group.groupId) != -1 ? true : false;
                    this.userProfileForm.addControl('orgGroupControl_' + group.groupId, this.formBuilder.control(isGroupOfUser ? true : ''));
                });
            },
            error: (err: any) => {
                console.log(err)
            }
        });
    }

    getOrgRoles() {
        this.organisationGroupService.getOrganisationRoles(this.organisationId).subscribe({
            next: (orgRoles: Role[]) => {
                this.orgRoles = orgRoles;
                this.orgRoles.map(role => {
                    let isRoleOfUser = this.userProfileResponseInfo.detail.roleIds &&
                        this.userProfileResponseInfo.detail.roleIds.indexOf(role.roleId) != -1 ? true : false;
                    this.userProfileForm.addControl('orgRoleControl_' + role.roleId, this.formBuilder.control(isRoleOfUser ? true : ''));
                });
            },
            error: (err: any) => {
                console.log(err)
            }
        });
    }

    ngAfterViewChecked() {
        if (!this.errorLinkClicked) {
            // This additional check has been done to avoid always scrolling to error summary because ngAfterViewChecked is triggered with dynamic form controls
            this.scrollHelper.doScroll();
        } else {
            this.errorLinkClicked = false;
        }
    }

    scrollToAnchor(elementId: string): void {
        this.errorLinkClicked = true; // Making the errorLinkClicked true to avoid scrolling to the error-summary
        this.viewportScroller.scrollToAnchor(elementId);
    }

    setFocus(inputIndex: number) {
        this.inputs.toArray()[inputIndex].nativeElement.focus();
    }

    public onSubmit(form: FormGroup) {

        this.submitted = true;
        if (this.formValid(form)) {
            this.userProfileRequestInfo.title = form.get('userTitle')?.value;
            this.userProfileRequestInfo.firstName = form.get('firstName')?.value;
            this.userProfileRequestInfo.lastName = form.get('lastName')?.value;
            this.userProfileRequestInfo.userName = form.get('userName')?.value;
            let identityProviderId = form.get('signInProviderControl')?.value || 0;
            this.userProfileRequestInfo.detail.identityProviderId = identityProviderId;

            this.userProfileRequestInfo.detail.groupIds = this.getSelectedGroupIds(form);


            this.userProfileRequestInfo.detail.roleIds = this.getSelectedRoleIds(form);

            if (this.isEdit) {
                this.updateUser();
            }
            else {
                this.createUser(form);
            }

        }
        else {
            this.scrollHelper.scrollToFirst('error-summary');
        }
    }

    getSelectedGroupIds(form: FormGroup) {
        let selectedGroupIds: number[] = [];
        this.orgGroups.map(group => {
            if (form.get('orgGroupControl_' + group.groupId)?.value === true) {
                selectedGroupIds.push(group.groupId);
            }
        });

        return selectedGroupIds;
    }

    getSelectedRoleIds(form: FormGroup) {
        let selectedRoleIds: number[] = [];
        this.orgRoles.map(role => {
            if (form.get('orgRoleControl_' + role.roleId)?.value === true) {
                selectedRoleIds.push(role.roleId);
            }
        });

        return selectedRoleIds;
    }

    updateUser() {
        this.wrapperUserService.updateUser(this.userProfileRequestInfo.userName, this.userProfileRequestInfo).subscribe({
            next: (userEditResponseInfo: UserEditResponseInfo) => {
                if (userEditResponseInfo.userId == this.userProfileRequestInfo.userName) {
                    this.submitted = false;
                    let data = {
                        'userName': this.userProfileRequestInfo.userName
                    };
                    this.router.navigateByUrl(`operation-success/${userEditResponseInfo.isRegisteredInIdam ? OperationEnum.UserUpdateWithIdamRegister : OperationEnum.UserUpdate}?data=` + JSON.stringify(data));
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

    createUser(form: FormGroup) {
        this.wrapperUserService.createUser(this.userProfileRequestInfo).subscribe({
            next: (userEditResponseInfo: UserEditResponseInfo) => {
                if (userEditResponseInfo.userId == this.userProfileRequestInfo.userName) {
                    this.submitted = false;
                    let data = {
                        'userName': this.userProfileRequestInfo.userName
                    };
                    this.router.navigateByUrl(`operation-success/${userEditResponseInfo.isRegisteredInIdam ? OperationEnum.UserCreateWithIdamRegister : OperationEnum.UserCreate}?data=` + JSON.stringify(data));
                }
                else {
                    console.log("Error in creating");
                }
            },
            error: (err: any) => {
                if (err.status == 409) {
                    form.controls['userName'].setErrors({ 'alreadyExists': true });
                    this.scrollHelper.scrollToFirst('error-summary');
                } else {
                    if (err.error = "INVALID_USER_ID") {
                        form.controls['userName'].setErrors({ 'invalidEmail': true });
                        this.scrollHelper.scrollToFirst('error-summary');
                    }
                }
                console.log(err);
            }
        });
    }

    formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        if (this.orgGroups != null && this.orgGroups != undefined && this.orgGroups != []) {
            let selectedGroupRoleIds: number[] = [];
            this.orgGroups.map(group => {
                if (form.get('orgGroupControl_' + group.groupId)?.value === true) {
                    selectedGroupRoleIds.push(group.groupId);
                }
            });
            this.orgRoles.map(role => {
                if (form.get('orgRoleControl_' + role.roleId)?.value === true) {
                    selectedGroupRoleIds.push(role.roleId);
                }
            });
            if (selectedGroupRoleIds.length <= 0) {
                form.setErrors({ groupRoleRequired: true });
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
        console.log("cancel");
        this.router.navigateByUrl('manage-users');
    }

    onResetPasswordClick() {
        let data = {
            'userName': this.editingUserName
        };
        this.router.navigateByUrl('manage-users/confirm-reset-password?data=' + JSON.stringify(data));
    }

    onDeleteClick() {
        let data = {
            'userName': this.editingUserName
        };
        this.router.navigateByUrl('manage-users/confirm-user-delete?data=' + JSON.stringify(data));
    }

    onGroupViewClick(groupId: any) {

        var formData: UserProfileResponseInfo = {
            title: this.userProfileForm.get('userTitle')?.value,
            firstName: this.userProfileForm.get('firstName')?.value,
            lastName: this.userProfileForm.get('lastName')?.value,
            userName: this.userProfileForm.get('userName')?.value,
            detail: {
                id: this.userProfileResponseInfo.detail.id,
                canChangePassword: this.userProfileResponseInfo.detail.canChangePassword,
                identityProviderId: this.userProfileForm.get('signInProviderControl')?.value || 0,
                groupIds: this.getSelectedGroupIds(this.userProfileForm),
                roleIds: this.getSelectedRoleIds(this.userProfileForm),
            },          
            organisationId: this.organisationId,
        };

        let data = {
            'isEdit': false,
            'groupId': groupId
        };
        this.router.navigateByUrl('manage-groups/view?data=' + JSON.stringify(data), { state: { 'formData': formData, 'routeUrl': this.router.url } });
    }
}

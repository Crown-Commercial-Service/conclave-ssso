import { Component, ElementRef, QueryList, ViewChildren } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { LocationStrategy, ViewportScroller } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { slideAnimation } from "src/app/animations/slide.animation";
import { UserGroup, UserProfileRequestInfo, UserProfileResponseInfo } from "src/app/models/user";
import { WrapperUserService } from "src/app/services/wrapper/wrapper-user.service";
import { WrapperUserContactService } from "src/app/services/wrapper/wrapper-user-contact.service";
import { ContactInfo, UserContactInfoList } from "src/app/models/contactInfo";
import { ActivatedRoute, Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";
import { WrapperOrganisationGroupService } from "src/app/services/wrapper/wrapper-org--group-service";
import { Role } from "src/app/models/organisationGroup";

@Component({
    selector: 'app-user-profile',
    templateUrl: './user-profile-component.html',
    styleUrls: ['./user-profile-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class UserProfileComponent extends BaseComponent implements OnInit {
    submitted!: boolean;
    userProfileForm!: FormGroup;
    userGroupTableHeaders = ['GROUPS'];
    userGroupColumnsToDisplay = ['group'];
    userRoleTableHeaders = ['ROLES'];
    userRoleColumnsToDisplay = ['accessRoleName'];
    contactTableHeaders = ['CONTACT_REASON', 'NAME', 'EMAIL', 'TELEPHONE_NUMBER'];
    contactColumnsToDisplay = ['contactReason', 'name', 'email', 'phoneNumber'];
    userGroups: UserGroup[] = [];
    userContacts: ContactInfo[] = [];
    userName: string;
    organisationId: string;
    canChangePassword: boolean = false;
    identityProviderDisplayName: string = '';
    roleDataList: any[] = [];
    routeStateData: any = {};

    @ViewChildren('input') inputs!: QueryList<ElementRef>;

    constructor(private userService: WrapperUserService, private userContactService: WrapperUserContactService, private locationStrategy: LocationStrategy,
        protected uiStore: Store<UIState>, private formBuilder: FormBuilder, private router: Router, private activatedRoute: ActivatedRoute,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private orgGroupService: WrapperOrganisationGroupService) {
        super(uiStore);
        this.userName = localStorage.getItem('user_name') || '';
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.routeStateData = this.router.getCurrentNavigation()?.extras.state;
        this.locationStrategy.onPopState(() => {
            this.onCancelClick();
        });
        this.userProfileForm = this.formBuilder.group({
            firstName: ['', Validators.compose([Validators.required])],
            lastName: ['', Validators.compose([Validators.required])],
        });
        this.viewportScroller.setOffset([100, 100]);
    }

    ngOnInit() {
        this.userService.getUser(this.userName).subscribe({
            next: (user: UserProfileResponseInfo) => {
                if (user != null) {
                    this.canChangePassword = user.detail.canChangePassword;
                    this.identityProviderDisplayName = user.detail.identityProviderDisplayName || '';
                    this.userGroups = user.detail.userGroups || [];
                    this.userGroups = this.userGroups.filter((group, index, self) =>
                        self.findIndex(t => t.groupId === group.groupId && t.group === group.group) === index);
                    if (this.routeStateData != undefined) {
                        this.userProfileForm.setValue({
                            firstName: this.routeStateData.firstName,
                            lastName: this.routeStateData.lastName
                        });
                    }
                    else {
                        this.userProfileForm.setValue({
                            firstName: user.firstName,
                            lastName: user.lastName
                        });
                    }
                    this.orgGroupService.getOrganisationRoles(this.organisationId).subscribe({
                        next: (orgRoles: Role[]) => {
                            user.detail.roleIds && user.detail.roleIds.map((roleId) => {
                                var orgRole = orgRoles.find(r => r.roleId == roleId);
                                if (orgRole) {
                                    this.roleDataList.push({ 'accessRoleName': orgRole.roleName });
                                }
                            });
                        },
                        error: (error: any) => {
                        }
                    });
                    this.getUserContact(this.userName);
                }
            },
            error: (error: any) => {
            }
        });
    }

    ngAfterViewChecked() {
        this.scrollHelper.doScroll();
    }

    scrollToAnchor(elementId: string): void {
        this.viewportScroller.scrollToAnchor(elementId);
    }

    setFocus(inputIndex: number) {
        this.inputs.toArray()[inputIndex].nativeElement.focus();
    }

    getUserContact(userName: string) {
        this.userContactService.getUserContacts(userName).subscribe({
            next: (userContactsInfo: UserContactInfoList) => {
                if (userContactsInfo != null) {
                    this.userContacts = userContactsInfo.contactsList;
                }
            },
            error: (error: any) => {
            }
        });
    }

    onChangePasswordClick() {
        this.router.navigateByUrl("change-password");
    }

    onRequestRoleChangeClick() {
        console.log("RoleChange");
    }

    onContactEditRow(dataRow: ContactInfo) {
        let data = {
            'isEdit': true,
            'userName': this.userName,
            'contactId': dataRow.contactId
        };
        this.router.navigateByUrl('user-contact-edit?data=' + JSON.stringify(data));
    }

    onContactAddClick() {
        let data = {
            'isEdit': false,
            'userName': this.userName,
            'contactId': 0
        };
        this.router.navigateByUrl('user-contact-edit?data=' + JSON.stringify(data));
    }

    onContactAssignRemoveClick() {
        console.log("Assign");
    }

    onSubmit(form: FormGroup) {
        this.submitted = true;
        if (this.formValid(form)) {
            this.submitted = false;

            let userRequest: UserProfileRequestInfo = {
                title: 0,
                organisationId: this.organisationId,
                userName: this.userName,
                detail: {
                    id: 0
                },
                firstName: form.get('firstName')?.value,
                lastName: form.get('lastName')?.value
            }
            this.userService.updateUser(this.userName, userRequest)
                .subscribe(
                    (data) => {
                        this.router.navigateByUrl(`operation-success/${OperationEnum.MyAccountUpdate}`);
                    },
                    (error) => {
                        console.log(error);
                        console.log(error.error);
                    });
        }
        else {
            this.scrollHelper.scrollToFirst('error-summary');
        }
    }

    formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        return form.valid;
    }

    onCancelClick() {
        this.router.navigateByUrl("home");
    }

    onGroupViewClick(event: any) {

        var formData = {
            'firstName': this.userProfileForm.get('firstName')?.value,
            'lastName': this.userProfileForm.get('lastName')?.value,
        };

        let data = {
            'isEdit': false,
            'groupId': event.groupId,
            'url': this.router.url
        };
        this.router.navigateByUrl('manage-groups/view?data=' + JSON.stringify(data), { state: { 'formData': formData, 'routeUrl': this.router.url } });
    }
}
import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { ViewportScroller } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { slideAnimation } from "src/app/animations/slide.animation";
import { UserGroup, UserProfileRequestInfo, UserProfileResponseInfo } from "src/app/models/user";
import { WrapperUserService } from "src/app/services/wrapper/wrapper-user.service";
import { WrapperUserContactService } from "src/app/services/wrapper/wrapper-user-contact.service";
import { ContactInfo, UserContactInfoList } from "src/app/models/userContact";
import { Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";

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
    userGroupTableHeaders = ['ROLES', 'GROUPS'];
    userGroupColumnsToDisplay = ['accessRole', 'group'];
    contactTableHeaders = ['NAME', 'EMAIL', 'TELEPHONE_NUMBER', 'FAX', 'WEB_URL', 'CONTACT_REASON'];
    contactColumnsToDisplay = ['name', 'email', 'phoneNumber', 'fax', 'webUrl', 'contactReason'];
    userGroups: UserGroup[] = [];
    userContacts: ContactInfo[] = [];
    userName: string;
    organisationId : string;
    canChangePassword: boolean = false;
    identityProviderDisplayName: string = '';

    constructor(private userService: WrapperUserService, private userContactService: WrapperUserContactService,
        protected uiStore: Store<UIState>, private formBuilder: FormBuilder, private router: Router,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper) {
        super(uiStore);
        this.userName = localStorage.getItem('user_name') || '';
        this.organisationId =localStorage.getItem('cii_organisation_id') || '';
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
                    this.canChangePassword = user.canChangePassword;
                    this.identityProviderDisplayName = user.identityProviderDisplayName || '';
                    this.userGroups = user.userGroups || [];                    
                    this.userProfileForm.setValue({
                        firstName: user.firstName,
                        lastName: user.lastName
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
        console.log(dataRow);
        let data = {
            'isEdit':true,
            'userName': this.userName,
            'contactId': dataRow.contactId
        };
        this.router.navigateByUrl('user-contact-edit?data=' + JSON.stringify(data));
    }

    onContactAddClick() {
        let data = {
            'isEdit':false,
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
                organisationId : this.organisationId,
                userName : this.userName,
                firstName: form.get('firstName')?.value,
                lastName: form.get('lastName')?.value
            }
            this.userService.updateUser(this.userName, userRequest, true)
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
}
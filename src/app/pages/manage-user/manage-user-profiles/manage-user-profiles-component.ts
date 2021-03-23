import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { ViewportScroller } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { slideAnimation } from "src/app/animations/slide.animation";
import { User, UserGroup, UserListInfo, UserListResponse, UserProfileRequestInfo } from "src/app/models/user";
import { WrapperUserService } from "src/app/services/wrapper/wrapper-user.service";
import { WrapperUserContactService } from "src/app/services/wrapper/wrapper-user-contact.service";
import { ContactInfo, UserContactInfoList } from "src/app/models/userContact";
import { Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";
import { WrapperOrganisationService } from "src/app/services/wrapper/wrapper-org-service";

@Component({
    selector: 'app-manage-user-profiles',
    templateUrl: './manage-user-profiles-component.html',
    styleUrls: ['./manage-user-profiles-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageUserProfilesComponent extends BaseComponent implements OnInit {
    userList: UserListResponse;
    organisationId: string;
    searchingUserName: string = "";
    currentPage: number = 1;
    totalPagesArray: number[] = [];
    pageSize: number = 10;
    usersTableHeaders = ['NAME', 'EMAIL'];
    usersColumnsToDisplay = ['name', 'userName'];

    constructor(private wrapperOrganisationService: WrapperOrganisationService,
        protected uiStore: Store<UIState>, private router: Router) {
        super(uiStore);
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.userList = {
            currentPage: this.currentPage,
            pageCount: 0,
            rowCount: 0,
            organisationId: this.organisationId,
            userList: []
        }
    }

    ngOnInit() {
        this.getOrganisationUsers()
    }

    getOrganisationUsers() {
        this.wrapperOrganisationService.getUsers(this.organisationId, this.searchingUserName, this.currentPage, this.pageSize).subscribe({
            next: (userListResponse: UserListResponse) => {
                if (userListResponse != null) {
                    this.userList = userListResponse;
                    this.totalPagesArray = Array(this.userList.pageCount).fill(0).map((x, i) => i + 1);
                }
            },
            error: (error: any) => {
            }
        });
    }

    onAddClick() {
        this.router.navigateByUrl("manage-users/add-user-selection");
    }

    searchTextChanged(event: any) {
        this.searchingUserName = event.target.value;
    }

    onSearchClick() {
        this.getOrganisationUsers();
    }

    setPage(pageNumber: any) {
        this.currentPage = pageNumber;
        console.log(this.currentPage);
        this.getOrganisationUsers();
    }

    onEditRow(dataRow: UserListInfo) {
        console.log(dataRow);
        let data = {
            'isEdit':true,
            'userName': dataRow.userName
        };
        this.router.navigateByUrl('manage-users/add-user/details?data=' + JSON.stringify(data));
    }
}
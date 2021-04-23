import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { ViewportScroller } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { slideAnimation } from "src/app/animations/slide.animation";
import { ActivatedRoute, Router } from "@angular/router";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";
import { WrapperOrganisationGroupService } from "src/app/services/wrapper/wrapper-org--group-service";
import { OrganisationGroupRequestInfo } from "src/app/models/organisationGroup";
import { WrapperOrganisationService } from "src/app/services/wrapper/wrapper-org-service";
import { CheckBoxUserListGridSource, UserListInfo, UserListResponse } from "src/app/models/user";
import { environment } from "src/environments/environment";

@Component({
    selector: 'app-manage-group-edit-users',
    templateUrl: './manage-group-edit-users-component.html',
    styleUrls: ['./manage-group-edit-users-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageGroupEditUsersComponent extends BaseComponent implements OnInit {
    organisationId: string;
    isEdit: boolean = false;
    editingGroupId: number = 0;
    groupName: string = '';
    userNames: string[] = [];
    addingUsers: UserListInfo[] = [];
    removingUsers: UserListInfo[] = [];

    searchingUserName: string = "";
    currentPage: number = 1;
    pageCount: number = 0;
    pageSize: number = environment.listPageSize;
    usersTableHeaders = ['NAME', 'EMAIL', 'SELECT_USER'];
    usersColumnsToDisplay = ['name', 'userName'];
    userGridSource: CheckBoxUserListGridSource[] = [];

    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private orgGroupService: WrapperOrganisationGroupService,
        private wrapperOrganisationService: WrapperOrganisationService) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            this.isEdit = routeData['isEdit'];
            this.editingGroupId = routeData['groupId'];
            this.userNames = routeData['userNames'];
            this.groupName = routeData['groupName'] || '';
            this.addingUsers = routeData['addingUsers'] || [];
            this.removingUsers = routeData['removingUsers'] || [];
        }
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.viewportScroller.setOffset([100, 100]);
    }

    ngOnInit() {
        this.getOrganisationUsers();
    }

    getOrganisationUsers() {
        this.wrapperOrganisationService.getUsers(this.organisationId, this.searchingUserName, this.currentPage, this.pageSize).subscribe({
            next: (userListResponse: UserListResponse) => {
                if (userListResponse != null) {
                    this.userGridSource = [];
                    this.pageCount = userListResponse.pageCount;
                    userListResponse.userList.map((orgUser: UserListInfo) => {
                        let isChecked = (this.userNames.findIndex(uName => uName == orgUser.userName) != -1 ||
                            this.addingUsers.findIndex(user => user.userName == orgUser.userName) != -1) &&
                            this.removingUsers.findIndex(user => user.userName == orgUser.userName) == -1;

                        let userGridSourceObject: CheckBoxUserListGridSource = {
                            name: orgUser.name,
                            userName: orgUser.userName,
                            isChecked: isChecked
                        };
                        this.userGridSource.push(userGridSourceObject);
                    });
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

    searchTextChanged(event: any) {
        this.searchingUserName = event.target.value;
    }

    onSearchClick() {
        this.currentPage = 1;
        this.getOrganisationUsers();
    }

    setPage(pageNumber: any) {
        this.currentPage = pageNumber;
        this.getOrganisationUsers();
    }

    onCheckBoxClickRow(dataRow: CheckBoxUserListGridSource) {
        if (dataRow.isChecked) {
            let inRemovedListIndex = this.removingUsers.findIndex(ru => ru.userName == dataRow.userName);
            if (inRemovedListIndex != -1) { // If in removed list removing from there
                this.removingUsers.splice(inRemovedListIndex, 1);
            }
            else {
                let userInfo: UserListInfo = {
                    name: dataRow.name,
                    userName: dataRow.userName
                };
                this.addingUsers.push(userInfo);
            }
        }
        else {
            let inAddedListIndex = this.addingUsers.findIndex(au => au.userName == dataRow.userName);
            if (inAddedListIndex != -1) { // If in added list removing from there
                this.removingUsers.splice(inAddedListIndex, 1);
            }
            else {
                let userInfo: UserListInfo = {
                    name: dataRow.name,
                    userName: dataRow.userName
                };
                this.removingUsers.push(userInfo);
            }
        }
    }

    onContinueClick() {
        let data = {
            'isEdit': this.isEdit,
            'groupId': this.editingGroupId,
            'userNames': this.userNames,
            'addingUsers': this.addingUsers,
            'removingUsers': this.removingUsers
        };
        this.router.navigateByUrl('manage-groups/edit-users-confirm?data=' + JSON.stringify(data));
    }

    onCancelClick() {
        let data = {
            'isEdit': true,
            'groupId': this.editingGroupId
        };
        this.router.navigateByUrl('manage-groups/view?data=' + JSON.stringify(data));
    }
}
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
import { CheckBoxRoleListGridSource, OrganisationGroupRequestInfo, Role } from "src/app/models/organisationGroup";

@Component({
    selector: 'app-manage-group-edit-roles',
    templateUrl: './manage-group-edit-roles-component.html',
    styleUrls: ['./manage-group-edit-roles-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageGroupEditRolesComponent extends BaseComponent implements OnInit {
    submitted!: boolean;
    organisationId: string;
    isEdit: boolean = false;
    editingGroupId: number = 0;
    groupName: string = '';
    roleIds: number[] = [];
    addingRoles: Role[] = [];
    removingRoles: Role[] = [];
    userCount: number = 0;

    searchText: string = "";
    rolesTableHeaders = ['NAME', 'SELECT_ROLE'];
    rolesColumnsToDisplay = ['roleName'];
    roleGridSource: CheckBoxRoleListGridSource[] = [];
    orgRoleList: Role[] = [];

    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private orgGroupService: WrapperOrganisationGroupService) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            this.isEdit = routeData['isEdit'];
            this.editingGroupId = routeData['groupId'];
            this.roleIds = routeData['roleIds'];
            this.groupName = routeData['groupName'] || '';
            this.addingRoles = routeData['addingRoles'] || [];
            this.removingRoles = routeData['removingRoles'] || [];
            this.userCount = routeData['userCount'] || 0;
        }
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        this.viewportScroller.setOffset([100, 100]);
    }

    ngOnInit() {
        this.getOrganisationRoles();
    }

    ngAfterViewChecked() {
        this.scrollHelper.doScroll();
    }

    scrollToAnchor(elementId: string): void {
        this.viewportScroller.scrollToAnchor(elementId);
    }

    searchTextChanged(event: any) {
        this.searchText = event.target.value;
    }

    onSearchClick() {
        console.log(this.searchText);
        this.setSearchResult();
    }

    setSearchResult() {
        this.roleGridSource = [];
        this.orgRoleList.map((orgRole: Role) => {
            if (this.searchText == '' || orgRole.roleName.toLowerCase().includes(this.searchText.toLowerCase())) {
                let isChecked = (this.roleIds.findIndex(rId => rId == orgRole.roleId) != -1 ||
                    this.addingRoles.findIndex(role => role.roleId == orgRole.roleId) != -1) &&
                    this.removingRoles.findIndex(role => role.roleId == orgRole.roleId) == -1;

                let roleGridSourceObject: CheckBoxRoleListGridSource = {
                    roleId: orgRole.roleId,
                    roleName: orgRole.roleName,
                    isChecked: isChecked
                };
                this.roleGridSource.push(roleGridSourceObject);
            }
        });
    }

    getOrganisationRoles() {
        this.orgGroupService.getOrganisationRoles(this.organisationId).subscribe({
            next: (roleListResponse: Role[]) => {
                if (roleListResponse != null) {
                    this.orgRoleList = roleListResponse;
                    this.setSearchResult();
                }
            },
            error: (error: any) => {
            }
        });
    }

    onCheckBoxClickRow(dataRow: CheckBoxRoleListGridSource) {
        if (dataRow.isChecked) {
            let inRemovedListIndex = this.removingRoles.findIndex(rr => rr.roleId == dataRow.roleId);
            if (inRemovedListIndex != -1) { // If in removed list removing from there
                this.removingRoles.splice(inRemovedListIndex, 1);
            }
            else {
                let roleInfo: Role = {
                    roleId: dataRow.roleId,
                    roleName: dataRow.roleName
                };
                this.addingRoles.push(roleInfo);
            }
        }
        else {
            let inAddedListIndex = this.addingRoles.findIndex(ar => ar.roleId == dataRow.roleId);
            if (inAddedListIndex != -1) { // If in added list removing from there
                this.addingRoles.splice(inAddedListIndex, 1);
            }
            else {
                let roleInfo: Role = {
                    roleId: dataRow.roleId,
                    roleName: dataRow.roleName
                };
                this.removingRoles.push(roleInfo);
            }
        }
    }

    onContinueClick() {
        let data = {
            'isEdit': this.isEdit,
            'groupId': this.editingGroupId,
            'roleIds': this.roleIds,
            'addingRoles': this.addingRoles,
            'removingRoles': this.removingRoles,
            'userCount': this.userCount
        };
        this.router.navigateByUrl('manage-groups/edit-roles-confirm?data=' + JSON.stringify(data));
    }

    onCancelClick() {
        let data = {
            'isEdit': true,
            'groupId': this.editingGroupId
        };
        this.router.navigateByUrl('manage-groups/view?data=' + JSON.stringify(data));
    }
}
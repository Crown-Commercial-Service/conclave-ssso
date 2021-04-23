import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { ViewportScroller, Location, LocationStrategy } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { slideAnimation } from "src/app/animations/slide.animation";
import { ActivatedRoute, Router } from "@angular/router";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";
import { WrapperOrganisationGroupService } from "src/app/services/wrapper/wrapper-org--group-service";
import { OrganisationGroupRequestInfo, OrganisationGroupResponseInfo } from "src/app/models/organisationGroup";
import { WrapperOrganisationService } from "src/app/services/wrapper/wrapper-org-service";
import { UserListInfo } from "src/app/models/user";

@Component({
    selector: 'app-manage-group-view',
    templateUrl: './manage-group-view-component.html',
    styleUrls: ['./manage-group-view-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageGroupViewComponent extends BaseComponent implements OnInit {
    organisationId: string;
    group: OrganisationGroupResponseInfo;
    isEdit: boolean = false;
    editingGroupId: number = 0;
    routeData: any = {};
    usersTableHeaders = ['NAME', 'EMAIL'];
    usersColumnsToDisplay = ['name', 'userId'];
    rolesTableHeaders = ['NAME'];
    roesColumnsToDisplay = ['name'];

    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper, private orgGroupService: WrapperOrganisationGroupService,
        private locationStrategy: LocationStrategy) {
        super(uiStore);
        this.group = {
            groupId: 0,
            groupName: '',
            roles: [],
            users: []
        };
        let queryParams = this.activatedRoute.snapshot.queryParams;
        let state = this.router.getCurrentNavigation()?.extras.state;
        localStorage.setItem('groupReadonlyViewState', JSON.stringify(state));
        this.locationStrategy.onPopState(() => {
            if (state) {
                this.onGoBackClick();
            }
        });

        if (queryParams.data) {
            this.routeData = JSON.parse(queryParams.data);
            this.isEdit = this.routeData['isEdit'];
            this.editingGroupId = this.routeData['groupId'];
        }
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
    }

    ngOnInit() {
        this.orgGroupService.getOrganisationGroup(this.organisationId, this.editingGroupId)
            .subscribe(
                (group: OrganisationGroupResponseInfo) => {
                    this.group = group;
                },
                (error) => {
                    console.log(error);
                    console.log(error.error);
                });
    }

    onNameEditClick() {
        let data = {
            'isEdit': this.isEdit,
            'groupId': this.editingGroupId,
            'groupName': this.group.groupName
        };
        this.router.navigateByUrl('manage-groups/edit-name?data=' + JSON.stringify(data));
    }

    onRoleEditClick() {
        let roleIds = this.group.roles.map(role => role.id);
        let data = {
            'isEdit': this.isEdit,
            'groupId': this.editingGroupId,
            'roleIds': roleIds,
            'groupName': this.group.groupName
        };
        this.router.navigateByUrl('manage-groups/edit-roles?data=' + JSON.stringify(data));
    }

    onUserEditClick() {
        let userNames = this.group.users.map(user => user.userId);
        let data = {
            'isEdit': this.isEdit,
            'groupId': this.editingGroupId,
            'userNames': userNames,
            'groupName': this.group.groupName
        };
        this.router.navigateByUrl('manage-groups/edit-users?data=' + JSON.stringify(data));
    }

    onDeleteClick() {
        console.log("delete");
        let data = {
            'isEdit': true,
            'organisationId': this.organisationId,
            'groupId': this.editingGroupId
        };
        this.router.navigateByUrl('manage-groups/delete-group-confirm?data=' + JSON.stringify(data));
    }

    onGoBackClick() {
        let stateString = localStorage.getItem('groupReadonlyViewState') || '';
        localStorage.removeItem('groupReadonlyViewState');
        let state = JSON.parse(stateString);
        let routeUrl = state.routeUrl;
        let formData = state.formData;

        this.router.navigateByUrl(`${routeUrl}`, { state: formData || {} });
    }
}
import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { slideAnimation } from "src/app/animations/slide.animation";
import { ActivatedRoute, Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { WrapperOrganisationGroupService } from "src/app/services/wrapper/wrapper-org--group-service";

@Component({
    selector: 'app-manage-group-delete-confirm',
    templateUrl: './manage-group-delete-confirm-component.html',
    styleUrls: ['./manage-group-delete-confirm-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageGroupDeleteConfirmComponent extends BaseComponent implements OnInit {
    isEdit: boolean = false;
    groupId: number = 0;
    organisationId: string = '';
    routeData: any = {};
    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private orgGroupService: WrapperOrganisationGroupService) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            this.routeData = JSON.parse(queryParams.data);
            this.isEdit = this.routeData['isEdit'];
            this.groupId = this.routeData['groupId'];
            this.organisationId = this.routeData['organisationId'];
        }
    }

    ngOnInit() {
    }

    onDeleteConfirmClick() {
        this.orgGroupService.deleteOrganisationGroup(this.organisationId, this.groupId).subscribe({
            next: () => { 
                this.router.navigateByUrl(`manage-groups/operation-success/${OperationEnum.GroupDelete}`);             
            },
            error: (error) => {
                console.log(error);
            }
        });
    }

    onCancelClick(){
        this.router.navigateByUrl('manage-groups/view?data=' + JSON.stringify(this.routeData));
    }
}
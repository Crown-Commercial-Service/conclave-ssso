import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { slideAnimation } from "src/app/animations/slide.animation";
import { ActivatedRoute, Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { WrapperUserService } from "src/app/services/wrapper/wrapper-user.service";

@Component({
    selector: 'app-manage-user-delete-confirm',
    templateUrl: './manage-user-delete-confirm-component.html',
    styleUrls: ['./manage-user-delete-confirm-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageUserDeleteConfirmComponent extends BaseComponent implements OnInit {
    userName: string = '';
    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private wrapperUserService: WrapperUserService) {
        super(uiStore);
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            console.log(routeData);
            this.userName = routeData['userName'];
        }
    }

    ngOnInit() {
    }

    onDeleteConfirmClick() {
        this.wrapperUserService.deleteUser(this.userName).subscribe({
            next: () => { 
                this.router.navigateByUrl(`operation-success/${OperationEnum.UserDelete}`);             
            },
            error: (error) => {
                console.log(error);
            }
        });
    }

    onCancelClick(){
        let data = {
            'isEdit':true,
            'userName': this.userName
        };
        this.router.navigateByUrl('manage-users/add-user/details?data=' + JSON.stringify(data));
    }
}
import { Component, OnInit} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';
import { AuthService } from 'src/app/services/auth/auth.service';

@Component({
    selector: 'app-operation-success',
    templateUrl: './operation-success.component.html',
    styleUrls: ['./operation-success.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class OperationSuccessComponent extends BaseComponent implements OnInit {
    operation : OperationEnum;
    operationEnum = OperationEnum;
    userName: string = '';

    constructor(private router: Router, private activatedRoute: ActivatedRoute,
        protected uiStore: Store<UIState>, private authService: AuthService) {
        super(uiStore);
        this.operation = parseInt(this.activatedRoute.snapshot.paramMap.get('operation') || '0');
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            this.userName = routeData['userName'];
        }
    }

    ngOnInit() {
    }

    onNavigateToProfileClick(){
        this.router.navigateByUrl("profile");
    }

    onNavigateToSignInClick(){
        this.authService.logOutAndRedirect();
    }

    onNavigateToManageUserClick(){
        this.router.navigateByUrl("manage-users");
    }
}

import { Component, OnInit} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';
import { AuthService } from 'src/app/services/auth/auth.service';

@Component({
    selector: 'app-operation-failed',
    templateUrl: './operation-failed.component.html',
    styleUrls: ['./operation-failed.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class OperationFailedComponent extends BaseComponent implements OnInit {
    operation : OperationEnum;
    operationEnum = OperationEnum;
    userName: string;
    messageKey: string;

    constructor(private router: Router,
        private route: ActivatedRoute, protected uiStore: Store<UIState>, private authService: AuthService) {
        super(uiStore);
        this.operation = parseInt(this.route.snapshot.paramMap.get('operation') || '0');
        let operationFailedState = this.router.getCurrentNavigation()?.extras.state;
        this.userName = operationFailedState && operationFailedState['userName'] || '';
        this.messageKey = operationFailedState && operationFailedState['messageKey'] || '';
    }

    ngOnInit() {
    }

    public onNavigateToSignInClick(){
        this.authService.logOutAndRedirect();
    }

    onNavigateToManageUserClick(){
        this.router.navigateByUrl("manage-users");
    }
}

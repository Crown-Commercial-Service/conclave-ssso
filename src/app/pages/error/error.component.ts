import { Component, ViewEncapsulation, ChangeDetectionStrategy, OnInit, OnDestroy, Self } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, FormArray, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { timeout } from 'rxjs/operators';
import * as _ from 'lodash';

import { BaseComponent } from 'src/app/components/base/base.component';
import { Data } from 'src/app/models/data';
import { dataService } from 'src/app/services/data/data.service';
import { UIState } from 'src/app/store/ui.states';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { AuthService } from 'src/app/services/auth/auth.service';
import { TokenInfo } from 'src/app/models/auth';
import { TokenService } from 'src/app/services/auth/token.service';
import { environment } from 'src/environments/environment';

@Component({
    templateUrl: './error.component.html',
    styleUrls: ['./error.component.scss']
})
export class ErrorComponent extends BaseComponent {

    public mainPageUrl: string = environment.uri.web.dashboard;
    public errorCode = '';

    constructor(private route: ActivatedRoute, protected uiStore: Store<UIState>,private authService: AuthService) {
        super(uiStore);
        this.route.queryParams.subscribe(params => {
            this.errorCode = params['error_description'];
        });
    }

    displayError(error: string) {
        var errorMessage = document.getElementById('error-message');
        if (errorMessage != undefined) {
            errorMessage.innerHTML = error;
            errorMessage.style.display = 'block';
        }
    }

    signoutUser() {
        // request for full session clean
        this.authService.logOutAndRedirect();
    }
}

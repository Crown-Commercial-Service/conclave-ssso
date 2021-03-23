import { Component, ViewEncapsulation, ChangeDetectionStrategy, OnInit, OnDestroy, Self } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, FormArray, Validators } from '@angular/forms';
import { Router } from '@angular/router';
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
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent extends BaseComponent implements OnInit {

    loginForm: FormGroup;
    loginErrorString!: string;
    submitted!: boolean;
    error!: string;
    loading = false;
    public externalAuthUrl: string =  `${environment.uri.api.security}/security/redirect_to_identity_provider`;
    
    constructor(private dataService: dataService,
        private formBuilder: FormBuilder,
        private translateService: TranslateService,
        private router: Router,
        private authService: AuthService,
        protected uiStore: Store<UIState>,
        private readonly tokenService : TokenService
    ) {
        super(uiStore);
        this.loginForm = this.formBuilder.group({
            email: ['', [Validators.required, Validators.email]],
            password: ['', Validators.compose([Validators.required, Validators.minLength(6)])]
        });
    }

    /**
    * @memberof LoginComponent
    */
    ngOnInit() {
        // this.translateService.get('LOGIN_ERROR').subscribe((value) => {
        //     this.loginErrorString = value;
        // });
        //window.location.href = this.authService.getAuthorizedEndpoint();
    }

    get f() { return this.loginForm.controls; }

    /**
    * Triggered each time the user clicks the submit button.
    *
    * @memberof LoginComponent
    */
    public onSubmit(form: FormGroup) {
        this.submitted = true;
        this.loading = true;
        if (this.formValid(form)) {
            this.authService.login(form.get('email')?.value, form.get('password')?.value).toPromise().then((tokenInfo: TokenInfo) => {
              if (tokenInfo.challengeRequired) {
                if (tokenInfo.challengeName === "NEW_PASSWORD_REQUIRED") {
                  localStorage.setItem('brickedon_user', JSON.stringify(form.get('email')?.value.replace('"', '').replace('"', '')));
                  localStorage.setItem('user_name', JSON.stringify(form.get('email')?.value.replace('"', '').replace('"', '')));
                  this.router.navigateByUrl('change-password?session=' + tokenInfo.sessionId);
                }
              } else {
                localStorage.setItem('brickedon_aws_tokens', JSON.stringify(tokenInfo));
                let idToken = this.tokenService.getDecodedIdToken(tokenInfo.idToken);
                localStorage.setItem('user_name', idToken.email);
                localStorage.setItem('brickedon_user', idToken.email);
                // this.router.navigateByUrl('home');
                window.location.href = '/home';
              }
            }, (err) => {
                this.error = 'Invalid Email or Password';
                this.loading = false;
            });
        }
    }

    /**
    * iterate through each form control and validate
    */
    public formValid(form: FormGroup): Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        return form.valid;
    }
}

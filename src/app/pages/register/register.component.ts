import { Component, ViewEncapsulation, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
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

@Component({
    selector: 'app-register',
    templateUrl: './register.component.html',
    styleUrls: ['./register.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent extends BaseComponent implements OnInit {

    registerForm!: FormGroup;
    signupErrorString!: string;
    submitted!: boolean;

    constructor(private dataService: dataService,
        private formBuilder: FormBuilder,
        // private spinnerService: SpinnerService,
        private translateService: TranslateService,
        // private utilitiesService: UtilitiesService,
        private router: Router,
        private authService: AuthService,
        protected uiStore: Store<UIState>
    ) {
        super(uiStore);
        this.registerForm = this.formBuilder.group({
            firstName: ['', Validators.compose([Validators.required])],
            lastName: ['', Validators.compose([Validators.required])],
            userName: ['', Validators.compose([Validators.required])],
            email: ['', Validators.compose([Validators.required])]
        });
    }

    /**
    * @memberof RegisterComponent
    */
    ngOnInit() {
        this.translateService.get('REGISTER_ERROR').subscribe((value) => {
            this.signupErrorString = value;
        });
    }

    /**
    * Triggered each time the user clicks the submit button.
    *
    * @memberof RegisterComponent
    */
    onSubmit(form: FormGroup): void {
        this.submitted = true;
        if (this.formValid(form)) {
            this.authService.register(form.get('firstName')?.value, form.get('lastName')?.value, form.get('userName')?.value, form.get('email')?.value).toPromise().then((response) => {
                console.log('---------REGISTER RESPONSE START---------');
                console.log(response);
                console.log('---------REGISTER RESPONSE FINISH--------');
                this.router.navigateByUrl('home');
            }, (err) => {
                console.log(err);
            });
        }
    }

    /**
    * iterate through each form control and validate
    */
    public formValid(form: FormGroup) : Boolean {
        if (form == null) return false;
        if (form.controls == null) return false;
        return form.valid;
        // let array = _.takeWhile(form.controls, function(c:FormControl) { return !c.valid; });
        // let array = _.takeWhile([], function(c:FormControl) { return !c.valid; });
        // return array.length > 0;
    }
}

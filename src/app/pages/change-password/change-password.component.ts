import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Location, ViewportScroller } from '@angular/common';
import * as _ from 'lodash';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { AuthService } from 'src/app/services/auth/auth.service';
import { PasswordChangeDetail } from 'src/app/models/passwordChangeDetail';
import { OperationEnum } from 'src/app/constants/enum';
import { ScrollHelper } from 'src/app/services/helper/scroll-helper.services';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ]
})
export class ChangePasswordComponent extends BaseComponent implements OnInit {

  formGroup: FormGroup;
  submitted: boolean = false;
  usedPasswordThreshold: number = environment.usedPasswordThreshold;


  constructor(private formBuilder: FormBuilder, private authService: AuthService,
    private router: Router, protected uiStore: Store<UIState>, private location: Location,
    private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper) {
    super(uiStore);
    this.formGroup = this.formBuilder.group({
      currentPassword: ['', Validators.compose([Validators.required])],
      newPassword: ['', Validators.compose([Validators.required, this.checkPasswordStrong])],
      confirmPassword: ['', Validators.compose([Validators.required])]
    }, { validators: this.checkPasswords });
  }

  ngOnInit() {
  }

  ngAfterViewChecked() {
    this.scrollHelper.doScroll();
  }

  public scrollToAnchor(elementId: string): void {
    this.viewportScroller.scrollToAnchor(elementId);
  }

  public checkPasswordStrong(control: FormControl) {
    var format = /[!@#$%^&*_]+/;
    let hasNumber = /\d/.test(control.value);
    let hasUpper = /[A-Z]/.test(control.value);
    let hasLower = /[a-z]/.test(control.value);
    let hasSpecial = format.test(control.value);
    let isInLength = control.value.length >= 10;
    const valid = hasNumber && hasUpper && hasLower && hasSpecial && isInLength;
    if (!valid) {
      return { 'pattern': true };
    }
    return null;
  }

  checkPasswords(form: FormGroup) {
    const newPassword = form.get('newPassword')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { errorConfirmPassword: true }
  }

  public onSubmit(form: FormGroup) {

    this.submitted = true;
    if (this.formValid(form)) {
      let userName = localStorage.getItem('user_name') || '';
      let contactData: PasswordChangeDetail = {
        oldPassword: form.get('currentPassword')?.value,
        newPassword: form.get('newPassword')?.value,
        userName: userName
      };

      this.authService.changePassword(contactData).toPromise()
        .then((response) => {
          console.log(response);
          this.authService.signOut();
          this.router.navigateByUrl(`change-password-success/${OperationEnum.PasswordChanged}`);
        }, (err) => {
          if (err.error == "INVALID_CURRENT_PASSWORD") {
            this.authService.signOut();
            this.router.navigateByUrl(`change-password-failed/${OperationEnum.PasswordChanged}`);
          }
          else if (err.error == "ERROR_PASSWORD_CONTAINS_USER_INFO") {
            form.controls['newPassword'].setErrors({ 'passwordContainsUserInfo': true });
          }
          else if (err.error == "ERROR_PASSWORD_ALREADY_USED") {
            form.controls['newPassword'].setErrors({ 'recentlyUsedPassword': true });
          }
          else {
            console.log(err);
          }
        });
    }
    else {
      this.scrollHelper.scrollToFirst('error-summary');
    }
  }

  public formValid(form: FormGroup): Boolean {
    if (form == null) return false;
    if (form.controls == null) return false;
    return form.valid;
  }

  public onCancelClick() {
    this.location.back();
  }
}

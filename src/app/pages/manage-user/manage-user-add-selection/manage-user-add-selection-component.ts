import { Component } from "@angular/core";
import { OnInit } from "@angular/core";
import { Store } from "@ngrx/store";
import { ViewportScroller } from '@angular/common';
import { BaseComponent } from "src/app/components/base/base.component";
import { UIState } from "src/app/store/ui.states";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { slideAnimation } from "src/app/animations/slide.animation";
import { User, UserGroup, UserListResponse, UserProfileRequestInfo } from "src/app/models/user";
import { WrapperUserService } from "src/app/services/wrapper/wrapper-user.service";
import { WrapperUserContactService } from "src/app/services/wrapper/wrapper-user-contact.service";
import { ContactInfo, UserContactInfoList } from "src/app/models/contactInfo";
import { Router } from "@angular/router";
import { OperationEnum } from "src/app/constants/enum";
import { ScrollHelper } from "src/app/services/helper/scroll-helper.services";
import { WrapperOrganisationService } from "src/app/services/wrapper/wrapper-org-service";

@Component({
    selector: 'app-manage-user-add-selection-component',
    templateUrl: './manage-user-add-selection-component.html',
    styleUrls: ['./manage-user-add-selection-component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ]
})
export class ManageUserAddSelectionComponent extends BaseComponent implements OnInit {
    submitted!: boolean;
    selectionForm!: FormGroup;

    constructor(private wrapperOrganisationService: WrapperOrganisationService,
        protected uiStore: Store<UIState>, private router: Router, private formBuilder: FormBuilder,
        private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper) {
        super(uiStore);
        this.selectionForm = this.formBuilder.group({
            selection: ['', Validators.compose([Validators.required])],
        });
    }

    ngOnInit() {
    }

    ngAfterViewChecked() {
        this.scrollHelper.doScroll();
    }

    public scrollToAnchor(elementId: string): void {
        this.viewportScroller.scrollToAnchor(elementId);
    }

    public onSubmit(form: FormGroup) {
        this.submitted = true;
        if (this.formValid(form)) {
            this.submitted = false;

            let selection = form.get('selection')?.value;
            if (selection === "singleUser"){
                this.router.navigateByUrl("manage-users/add-user/details");
            }
            else{
                console.log("Add Multiple Users Selected");
            }
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
}
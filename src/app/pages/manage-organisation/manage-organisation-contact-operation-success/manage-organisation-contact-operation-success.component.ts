import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import {Location} from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { contactService } from 'src/app/services/contact/contact.service';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';

@Component({
    selector: 'app-manage-organisation-contact-operation-success',
    templateUrl: './manage-organisation-contact-operation-success.component.html',
    styleUrls: ['./manage-organisation-contact-operation-success.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Default
})
export class ManageOrganisationContactOperationSuccessComponent extends BaseComponent implements OnInit {

    organisationId: number;
    operation : OperationEnum;
    operationEnum = OperationEnum;

    constructor(private contactService: contactService, private router: Router,
        private route: ActivatedRoute, protected uiStore: Store<UIState>) {
        super(uiStore);
        this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId') || '0');
        this.operation = parseInt(this.route.snapshot.paramMap.get('operation') || '0');
    }

    ngOnInit() {
    }

    public onNavigateToProfileClick(){
        this.router.navigateByUrl(`manage-org/profile`);
    }
}

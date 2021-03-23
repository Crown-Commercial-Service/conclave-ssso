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
    selector: 'app-manage-organisation-contact-delete',
    templateUrl: './manage-organisation-contact-delete.component.html',
    styleUrls: ['./manage-organisation-contact-delete.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Default
})
export class ManageOrganisationContactDeleteComponent extends BaseComponent implements OnInit {

    organisationId: number;
    contactId: number;

    constructor(private contactService: contactService, private router: Router, private location: Location,
        private route: ActivatedRoute, protected uiStore: Store<UIState>) {
        super(uiStore);
        this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId') || '0');
        this.contactId = parseInt(this.route.snapshot.paramMap.get('contactId') || '0');
    }

    ngOnInit() {
    }

    public onBackClick(){
        this.location.back();
    }

    public onConfirmClick() {
        this.contactService.deleteContact(this.contactId).subscribe(() => {
            this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/contact-operation-success/${OperationEnum.Delete}`);
        });
    }

    public onCancelClick(){
        this.location.back();
    }

}

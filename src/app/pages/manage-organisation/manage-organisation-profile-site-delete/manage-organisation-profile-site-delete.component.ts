import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';
import { WrapperOrganisationSiteService } from 'src/app/services/wrapper/wrapper-org-site-service';

@Component({
  selector: 'app-manage-organisation-profile-site-delete',
  templateUrl: './manage-organisation-profile-site-delete.component.html',
  styleUrls: ['./manage-organisation-profile-site-delete.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationSiteDeleteComponent extends BaseComponent implements OnInit {

  organisationId: string;
  siteId: number = 0;
    constructor(protected uiStore: Store<UIState>, private router: Router, private activatedRoute: ActivatedRoute,
        private contactService: WrapperOrganisationSiteService) {
        super(uiStore);
        this.organisationId = localStorage.getItem('cii_organisation_id') || '';
        let queryParams = this.activatedRoute.snapshot.queryParams;
        if (queryParams.data) {
            let routeData = JSON.parse(queryParams.data);
            console.log(routeData);
            this.siteId = routeData['siteId'];
        }
    }

    ngOnInit() {
    }

    onDeleteConfirmClick() {
        this.contactService.deleteOrganisationSite(this.organisationId, this.siteId).subscribe({
            next: () => { 
                this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.DeleteSite}`);           
            },
            error: (error : any) => {
                console.log(error);
            }
        });
    }

    onCancelClick(){
        let data = {
            'isEdit': true,
            'siteId': this.siteId
        };
        this.router.navigateByUrl('manage-org/profile/site/edit?data=' + JSON.stringify(data));
    }

}

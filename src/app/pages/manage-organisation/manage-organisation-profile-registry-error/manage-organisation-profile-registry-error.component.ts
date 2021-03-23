import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { timeout } from 'rxjs/operators';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { Location } from '@angular/common';

import { BaseComponent } from 'src/app/components/base/base.component';
import { Data } from 'src/app/models/data';
import { dataService } from 'src/app/services/data/data.service';
import { UIState } from 'src/app/store/ui.states';

@Component({
  selector: 'app-manage-organisation-profile-registry-error',
  templateUrl: './manage-organisation-profile-registry-error.component.html',
  styleUrls: ['./manage-organisation-profile-registry-error.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationRegistryErrorComponent extends BaseComponent implements OnInit {

  public reason!: string;
  public organisationId!: number;

  constructor(private dataService: dataService, private router: Router, private route: ActivatedRoute, private location: Location, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.organisationId = JSON.parse(localStorage.getItem('organisation_id') + '');
    // this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId') || '0');
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params.reason) {
        this.reason = params.reason;
      } else {
        this.reason = 'unknown';
      }
    });
  }

  public goBack(){
    this.location.back();
  }

}

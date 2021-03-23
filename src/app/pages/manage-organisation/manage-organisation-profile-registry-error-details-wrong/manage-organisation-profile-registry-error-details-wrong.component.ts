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
  selector: 'app-manage-organisation-profile-registry-error-details-wrong',
  templateUrl: './manage-organisation-profile-registry-error-details-wrong.component.html',
  styleUrls: ['./manage-organisation-profile-registry-error-details-wrong.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationRegistryDetailsWrongComponent extends BaseComponent implements OnInit {

  public organisationId!: number;

  constructor(private dataService: dataService, private location: Location, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.organisationId = parseInt(this.route.snapshot.paramMap.get('organisationId') || '0');
  }

  ngOnInit() { }

  public goBack(){
    this.location.back();
  }

}

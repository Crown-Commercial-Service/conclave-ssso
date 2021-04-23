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
  selector: 'app-manage-organisation-error',
  templateUrl: './manage-organisation-error.component.html',
  styleUrls: ['./manage-organisation-error.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationErrorComponent extends BaseComponent implements OnInit {

  public reason!: string;

  constructor(private dataService: dataService, private router: Router, private route: ActivatedRoute, private location: Location, protected uiStore: Store<UIState>) {
    super(uiStore);
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

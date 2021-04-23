import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { timeout } from 'rxjs/operators';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { dataService } from 'src/app/services/data/data.service';
import { UIState } from 'src/app/store/ui.states';

@Component({
  selector: 'app-nominate-success',
  templateUrl: './nominate-success.component.html',
  styleUrls: ['./nominate-success.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NominateSuccessComponent extends BaseComponent implements OnInit {

  public emailAddress: string = '';

  constructor(private dataService: dataService, private router: Router, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.emailAddress = localStorage.getItem('brickendon_nominate_email_address')+'';
  }

  ngOnInit() { }

}

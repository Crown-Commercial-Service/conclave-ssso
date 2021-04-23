import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { Store } from '@ngrx/store';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, map, share, timeout } from 'rxjs/operators';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { Scheme } from '../../../models/scheme';
import { UIState } from 'src/app/store/ui.states';
import { ciiService } from 'src/app/services/cii/cii.service';

@Component({
    selector: 'app-manage-organisation-registration-step-2',
    templateUrl: './manage-organisation-registration-step-2.component.html',
    styleUrls: ['./manage-organisation-registration-step-2.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrgRegStep2Component extends BaseComponent implements OnInit {

  public items$!: Observable<any>;
  public scheme!: string;
  public schemeSubject: BehaviorSubject<string> = new BehaviorSubject<string>('GB-COH');
  public schemeSubjectObs: Observable<string> = this.schemeSubject.asObservable();
  public schemeName!: string;
  public txtValue!: string;
  submitted: boolean = false;
  
  constructor(private ref: ChangeDetectorRef, private ciiService: ciiService, private router: Router, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.txtValue = '';
  }

  ngOnInit() {
    this.items$ = this.ciiService.getSchemes().pipe(share());
    this.items$.subscribe({
      next: result => {
        console.log(result);
        if(!result.error) {
          this.scheme = result[0].scheme;
          this.schemeName = result[0].schemeName;
          localStorage.setItem('scheme_name', JSON.stringify(this.schemeName));
        }
      }
    });
  }

  public onSubmit() {
    this.submitted = true;
    if (this.txtValue && this.txtValue.length > 0) {
      // localStorage.setItem('scheme_name', JSON.stringify(this.schemeName));
      this.router.navigateByUrl(`manage-org/register/search/${this.scheme}/${this.txtValue}`);
    }
  }

  public onSelect(item: any) {
    this.schemeSubject.next(item.scheme);
    var el = document.getElementById(item.scheme) as HTMLInputElement;
    if (el) {
      el.checked = true;
    }
    this.scheme === item.scheme;
    this.submitted = false;
    this.txtValue == '';
    localStorage.setItem('scheme_name', JSON.stringify(item.schemeName));
    setTimeout(() => {
      this.ref.detectChanges();
    }, 5000);
  }

}

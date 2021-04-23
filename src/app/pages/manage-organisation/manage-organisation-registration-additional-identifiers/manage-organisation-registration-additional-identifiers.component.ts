import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { Store } from '@ngrx/store';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError, map, share, timeout } from 'rxjs/operators';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { Scheme } from '../../../models/scheme';
import { UIState } from 'src/app/store/ui.states';
import { ciiService } from 'src/app/services/cii/cii.service';

@Component({
    selector: 'app-manage-organisation-registration-additional-identifiers',
    templateUrl: './manage-organisation-registration-additional-identifiers.component.html',
    styleUrls: ['./manage-organisation-registration-additional-identifiers.component.scss'],
    animations: [
        slideAnimation({
            close: { 'transform': 'translateX(12.5rem)' },
            open: { left: '-12.5rem' }
        })
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrgRegAdditionalIdentifiersComponent extends BaseComponent implements OnInit {

  public item$!: Observable<any>;
  public orgGroup: string = 'manage-org/register/user';
  public schemeName: string = '';
  public selectedIdentifiers: any[] = new Array();
  public additionalIdentifiers: any[] = new Array();
  public routeParams!: any;
  public organisation!:any;

  constructor(private ciiService: ciiService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {
    this.schemeName = JSON.parse(localStorage.getItem('scheme_name')+'').replace('"','').replace('"','');
    this.route.params.subscribe(params => {
      this.routeParams = params;
      this.organisation = JSON.parse(localStorage.getItem('cii_organisation')+'');
      if (this.organisation) {
        this.selectedIdentifiers = [...this.organisation.additionalIdentifiers];
        // this.additionalIdentifiers = this.organisation.additionalIdentifiers;
        // this.selectedIdentifiers = this.organisation.additionalIdentifiers;
        // Object.assign(this.organisation.additionalIdentifiers, this.selectedIdentifiers);
      } else {
        this.router.navigateByUrl(`manage-org/register/error/notfound`);
      }
    });
  }

  public goBack() {
    this.router.navigateByUrl('manage-org/register/search/' + this.routeParams.scheme + '/' + this.routeParams.id);
  }

  public onSubmit() {
    const org = JSON.parse(localStorage.getItem('cii_organisation')+'');
    org.additionalIdentifiers = this.selectedIdentifiers;
    localStorage.setItem('cii_organisation', JSON.stringify(org));
    this.router.navigateByUrl(this.orgGroup);
  }

  public onChange(event: any, additionalIdentifier: any) {
    if (event.currentTarget.checked) {
      this.selectedIdentifiers.push(additionalIdentifier);
    } else {
      for (let i = 0; i < this.selectedIdentifiers.length; i++) { 
        if (this.selectedIdentifiers[i].id === additionalIdentifier.id) { 
          this.selectedIdentifiers.splice(i, 1); 
        }
      }
    }
  }

  public getSchemaName(schema: string): string {
    switch(schema) { 
      case 'GB-COH': { 
         return 'Companies House';
      } 
      case 'US-DUN': { 
         return 'Dun and Bradstreet';
      }
      case 'GB-CHC': { 
        return 'Charities Commission for England and Wales';
      }
      case 'GB-SC': { 
        return 'Scottish Charities Commission';
      }
      case 'GB-NIC': { 
        return 'Northern Ireland Charities Commission';
      }
      default: { 
         return '';
      } 
   } 
  }

}

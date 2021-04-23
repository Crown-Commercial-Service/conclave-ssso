import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { UIState } from 'src/app/store/ui.states';
import { AuthService } from 'src/app/services/auth/auth.service';
import { ciiService } from 'src/app/services/cii/cii.service';
import { UserService } from 'src/app/services/postgres/user.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { contactService } from 'src/app/services/contact/contact.service';
import { ContactType } from 'src/app/models/contactDetail';
import { environment } from "src/environments/environment";
import { Observable } from 'rxjs';
import { filter, map, share } from 'rxjs/operators';
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';

@Component({
  selector: 'app-buyer-details',
  templateUrl: './details.component.html',
  styleUrls: ['./details.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BuyerDetailsComponent extends BaseComponent implements OnInit {

  public org$!: Observable<any>;
  public registries$!: Observable<any>;
  public additionalIdentifiers$!: Observable<any>;
  public selectedOrgId!: string;

  constructor(private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private wrapperOrgService: WrapperOrganisationService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params.id) {
        this.selectedOrgId = params.id;
        this.org$ = this.organisationService.getById(params.id).pipe(share());
        this.registries$ = this.ciiService.getOrgs(params.id).pipe(share());
        this.registries$.subscribe({
          next: result => {
            this.additionalIdentifiers$ = Observable.of(result[0].additionalIdentifiers);
          }
        });
      }
    });
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

  public onContinueClick() {
    this.router.navigateByUrl(`buyer/confirm/${this.selectedOrgId}`);
  }

  public onCancelClick() {
    this.router.navigateByUrl('buyer/search');
  }
}

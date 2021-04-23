import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError, map, timeout } from 'rxjs/operators';

import { BaseComponent } from 'src/app/components/base/base.component';
import { slideAnimation } from 'src/app/animations/slide.animation';
import { Scheme } from '../../../models/scheme';
import { UIState } from 'src/app/store/ui.states';
import { AuthService } from 'src/app/services/auth/auth.service';
import { ciiService } from 'src/app/services/cii/cii.service';
import { UserService } from 'src/app/services/postgres/user.service';
import { OrganisationService } from 'src/app/services/postgres/organisation.service';
import { contactService } from 'src/app/services/contact/contact.service';
import { ContactType } from 'src/app/models/contactDetail';

@Component({
  selector: 'app-manage-organisation-registration-rightToBuy',
  templateUrl: './manage-organisation-registration-type.component.html',
  styleUrls: ['./manage-organisation-registration-type.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrgRegRightToBuyComponent extends BaseComponent implements OnInit {

  defaultChoice: string = "supplier";

  constructor(private formBuilder: FormBuilder, private translateService: TranslateService, private authService: AuthService, private ciiService: ciiService, private userService: UserService, private organisationService: OrganisationService, private contactService: contactService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
  }

  ngOnInit() {

  }

  public onSubmit() {
    localStorage.setItem("manage-org_reg_type", this.defaultChoice);
    let regType = localStorage.getItem("manage-org_reg_type")+'';
    if (regType !== 'supplier') {
      this.router.navigateByUrl('manage-org/register/buyer-type');
    } else {
      this.router.navigateByUrl(`manage-org/register/start`);
    }
  }
}

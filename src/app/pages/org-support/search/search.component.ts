import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
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
import { TokenService } from 'src/app/services/auth/token.service';

@Component({
  selector: 'app-org-support-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgSupportSearchComponent extends BaseComponent implements OnInit {

  formGroup: FormGroup;
  public data$!: Observable<any>;
  public selectedRow!: string;
  public selectedRowId: string = '';
  public accesstoken: any;
  public data!: [];
  public pageOfItems!: any;
  public organisationId!: number;

  constructor(private cf: ChangeDetectorRef, private formBuilder: FormBuilder, private translateService: TranslateService, private organisationService: OrganisationService, private wrapperOrganisationService: WrapperOrganisationService, private readonly tokenService: TokenService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>) {
    super(uiStore);
    this.accesstoken = this.tokenService.getDecodedAccessToken();
    this.formGroup = this.formBuilder.group({
      search: [, Validators.compose([Validators.required])],
    });
  }

  ngOnInit() {
    this.organisationService.getById(this.accesstoken.ciiOrgId).subscribe(org => {
      if (org) {
        this.organisationId = org.organisationId;
        // this.data$ = this.wrapperOrganisationService.getUsers(this.accesstoken.ciiOrgId, '', 0, 25).pipe(share());
        this.data$ = this.organisationService.getUsers().pipe(share());
        this.data$.subscribe({
          next: results => {
            this.data = results;
            this.pageOfItems = this.data.slice(0, 10);
            // this.selectedRow = results[0];
            // this.selectedRowId = results[0].id;
            setTimeout(() => {
              this.cf.detectChanges();
            }, 100);
          }
        });
      }
    });
  }

  onSearch() {
    this.data$ = this.organisationService.getUsers().pipe(
      map(items => items.filter((item: any) => 
        item.organisationLegalName.toLowerCase().includes((this.formGroup.get('search')?.value)+''.toLowerCase()) ||
        item.userName.toLowerCase().includes((this.formGroup.get('search')?.value)+''.toLowerCase()) ||
        item.name.toLowerCase().includes((this.formGroup.get('search')?.value)+''.toLowerCase())
      )),
    );
    this.data$.subscribe({
      next: results => {
        // if (results.length > 0) {
          this.data = results;
          this.pageOfItems = this.data.slice(0, 10);
          // if(results.length > 0) {
          //   this.selectedRow = results[0];
          //   this.selectedRowId = results[0].userName;
          // }
        // }
        setTimeout(() => {
          this.cf.detectChanges();
        }, 100);
      }
    });
  }

  public onSelect(item: any) {
    this.selectedRow = item;
    this.selectedRowId = item.userName;
  }

  public onContinueClick() {
    this.router.navigateByUrl(`org-support/details/${this.selectedRowId}`);
  }

  public onCancelClick() {
    this.router.navigateByUrl('home');
  }

  onChangePage(pageOfItems: Array<any>) {
    this.pageOfItems = pageOfItems;
  }

}

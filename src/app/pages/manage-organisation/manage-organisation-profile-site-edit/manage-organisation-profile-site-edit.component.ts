import { ChangeDetectionStrategy, Component, ElementRef, OnInit, QueryList, ViewChildren, ViewEncapsulation } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Location, ViewportScroller } from '@angular/common';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { ContactDetails, ContactType } from 'src/app/models/contactDetail';
import { contactService } from 'src/app/services/contact/contact.service';
import { UIState } from 'src/app/store/ui.states';
import { OperationEnum } from 'src/app/constants/enum';
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';
import { TokenService } from 'src/app/services/auth/token.service';
import { WrapperOrganisationSiteService } from 'src/app/services/wrapper/wrapper-org-site-service';
import { OrganisationSiteInfo, OrganisationSiteResponse } from 'src/app/models/site';
import { ScrollHelper } from 'src/app/services/helper/scroll-helper.services';
import { WrapperSiteContactService } from 'src/app/services/wrapper/wrapper-site-contact-service';
import { ContactInfo, SiteContactInfoList } from 'src/app/models/contactInfo';

@Component({
  selector: 'app-manage-organisation-profile-site-edit',
  templateUrl: './manage-organisation-profile-site-edit.component.html',
  styleUrls: ['./manage-organisation-profile-site-edit.component.scss'],
  animations: [
    slideAnimation({
      close: { 'transform': 'translateX(12.5rem)' },
      open: { left: '-12.5rem' }
    })
  ]
})
export class ManageOrganisationSiteEditComponent extends BaseComponent implements OnInit {
  siteForm: FormGroup;
  submitted!: boolean;
  isEdit: boolean = false;
  siteId: number = 0;
  organisationId: string;
  contactTableHeaders = ['CONTACT_REASON', 'NAME', 'EMAIL', 'TELEPHONE_NUMBER'];
  contactColumnsToDisplay = ['contactReason', 'name', 'email', 'phoneNumber'];
  contactData: ContactInfo[];

  @ViewChildren('input') inputs!: QueryList<ElementRef>;

  constructor(private formBuilder: FormBuilder, private router: Router, private activatedRoute: ActivatedRoute,
    protected uiStore: Store<UIState>, private viewportScroller: ViewportScroller, private scrollHelper: ScrollHelper,
    private orgSiteService: WrapperOrganisationSiteService, private siteContactService: WrapperSiteContactService) {
    super(uiStore);
    let queryParams = this.activatedRoute.snapshot.queryParams;
    if (queryParams.data) {
      let routeData = JSON.parse(queryParams.data);
      this.isEdit = routeData['isEdit'];
      this.siteId = routeData['siteId'];
    }
    this.contactData = [];
    this.organisationId = localStorage.getItem('cii_organisation_id') || '';
    this.siteForm = this.formBuilder.group({
      name: ['', Validators.compose([Validators.required])],
      streetAddress: ['', Validators.compose([Validators.required])],
      locality: ['', null],
      region: ['', null],
      postalCode: ['', Validators.compose([Validators.required])],
      countryCode: ['', Validators.compose([Validators.required])],
    });
  }

  ngOnInit() {
    if (this.isEdit) {
      this.orgSiteService.getOrganisationSite(this.organisationId, this.siteId).subscribe(
        {
          next: (siteInfo: OrganisationSiteResponse) => {
            this.siteForm.setValue({
              name: siteInfo.siteName,
              streetAddress: siteInfo.address.streetAddress,
              locality: siteInfo.address.locality,
              region: siteInfo.address.region,
              postalCode: siteInfo.address.postalCode,
              countryCode: siteInfo.address.countryCode,
            });
            this.getSiteContacts();
          },
          error: (error: any) => {
            console.log(error);
          }
        });
    }
  }

  getSiteContacts() {
    this.siteContactService.getSiteContacts(this.organisationId, this.siteId).subscribe({
      next: (siteContactListInfo: SiteContactInfoList) => {
        if (siteContactListInfo != null) {
          this.contactData = siteContactListInfo.siteContacts;
        }
      },
      error: (error: any) => {
        console.log(error);
      }
    });
  }

  ngAfterViewChecked() {
    this.scrollHelper.doScroll();
  }

  scrollToAnchor(elementId: string): void {
    this.viewportScroller.scrollToAnchor(elementId);
  }

  setFocus(inputIndex: number) {
    this.inputs.toArray()[inputIndex].nativeElement.focus();
  }

  public onSubmit(form: FormGroup) {
    this.submitted = true;
    if (this.formValid(form)) {
      let orgSiteInfo: OrganisationSiteInfo = {
        siteName: form.get('name')?.value,
        address: {
          streetAddress: form.get('streetAddress')?.value,
          locality: form.get('locality')?.value,
          region: form.get('region')?.value,
          postalCode: form.get('postalCode')?.value,
          countryCode: form.get('countryCode')?.value,
        }
      };

      if (this.isEdit) {
        this.orgSiteService.updateOrganisationSite(this.organisationId, this.siteId, orgSiteInfo).subscribe(
          {
            next: () => {
              this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.UpdateSite}`);
              this.submitted = false;
            },
            error: (error: any) => {
              console.log(error);
            }
          });
      }
      else {
        {
          this.orgSiteService.createOrganisationSite(this.organisationId, orgSiteInfo).subscribe(
            {
              next: () => {
                this.router.navigateByUrl(`manage-org/profile/contact-operation-success/${OperationEnum.CreateSite}`);
                this.submitted = false;
              },
              error: (error: any) => {
                console.log(error);
              }
            });
        }
      }
    }
    else {
      this.scrollHelper.scrollToFirst('error-summary');
    }
  }

  public formValid(form: FormGroup): Boolean {
    if (form == null) return false;
    if (form.controls == null) return false;
    return form.valid;
  }

  onCancelClick() {
    this.router.navigateByUrl('manage-org/profile');
  }

  onDeleteClick() {
    let data = {
      'organisationId': this.organisationId,
      'siteId': this.siteId
    };
    this.router.navigateByUrl('manage-org/profile/site/delete?data=' + JSON.stringify(data));
  }

  public onContactAddClick() {
    let data = {
      'isEdit': false,
      'contactId': 0,
      'siteId': this.siteId
    };
    this.router.navigateByUrl('manage-org/profile/contact-edit?data=' + JSON.stringify(data));
  }

  onContactEditClick(contactInfo: ContactInfo) {
    let data = {
      'isEdit': true,
      'contactId': contactInfo.contactId,
      'siteId': this.siteId
    };
    this.router.navigateByUrl('manage-org/profile/contact-edit?data=' + JSON.stringify(data));
  }
}

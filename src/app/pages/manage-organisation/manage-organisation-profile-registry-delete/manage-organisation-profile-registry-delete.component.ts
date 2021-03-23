import { ChangeDetectionStrategy, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { share, timeout } from 'rxjs/operators';
import { slideAnimation } from 'src/app/animations/slide.animation';

import { BaseComponent } from 'src/app/components/base/base.component';
import { Data } from 'src/app/models/data';
import { dataService } from 'src/app/services/data/data.service';
import { UIState } from 'src/app/store/ui.states';
import { ciiService } from 'src/app/services/cii/cii.service';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { WrapperUserService } from 'src/app/services/wrapper/wrapper-user.service';
import { User } from 'src/app/models/user';
import { TokenService } from 'src/app/services/auth/token.service';

@Component({
  selector: 'app-manage-organisation-profile-registry-delete',
  templateUrl: './manage-organisation-profile-registry-delete.component.html',
  styleUrls: ['./manage-organisation-profile-registry-delete.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationRegistryDeleteComponent extends BaseComponent implements OnInit {

  public item$!: Observable<any>;
  public organisationId: string;
  public routeParams!: any;

  constructor(private ciiService: ciiService, private wrapperService: WrapperUserService, private dataService: dataService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService) {
    super(uiStore);
    this.organisationId = JSON.parse(localStorage.getItem('organisation_id') + '');
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.routeParams = params;
      if (params.id && params.scheme) {
        this.item$ = this.ciiService.getIdentifiers(this.tokenService.getDecodedAccessToken().ciiOrgId, params.scheme, params.id).pipe(share());
        // this.item$.subscribe({
        //   next: result => {
        //     if (result.error) {
        //       if (result.message == 'Error 400') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/notfound`);
        //       } else if (result.message == 'Error 401') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       } else if (result.message == 'Error 403') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       } else if (result.message == 'Error 404') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/notfound`);
        //       } else if (result.message == 'Error 405') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/`);
        //       } else {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       }
        //     } else {
        //       // this.selectedIdentifiers = result.additionalIdentifiers;
        //       // localStorage.setItem('cii_organisation', JSON.stringify(result));
        //     }
        //   }, error: err => {
        //     if (err.status) {
        //       if (err.status == '400') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/notfound`);
        //       } else if (err.status == '401') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       } else if (err.status == '403') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       } else if (err.status == '404') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/notfound`);
        //       } else if (err.status == '405') {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/`);
        //       } else {
        //         this.router.navigateByUrl(`manage-org/profile/${this.organisationId}/registry/error/generic`);
        //       }
        //     } else {
        //       this.router.navigateByUrl(`manage-org/register/error`);
        //     }
        //   }
        // });
      }
    });
  }

  public onSubmit() {
    this.wrapperService.getUser(localStorage.getItem('user_name')+'').subscribe({
      next: (user: User) => {
          if (user != null) { 
            this.ciiService.deleteScheme(this.tokenService.getDecodedAccessToken().ciiOrgId, this.routeParams.scheme, this.routeParams.id).subscribe((data) => {
              this.router.navigateByUrl('manage-org/profile/' + this.organisationId + '/registry/delete/confirmation/' + this.routeParams.scheme + '/' + this.routeParams.id);
            }, (error) => {
                console.log(error);
            });                 
          } else {
              console.log('no user found from wrapper service');
          }
      }, error: (error: any) => {
          console.log(error);
      }
    });
  }

  public goBack() {
    this.router.navigateByUrl('manage-org/profile');
  }

}

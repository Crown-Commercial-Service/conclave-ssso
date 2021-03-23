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
import { WrapperOrganisationService } from 'src/app/services/wrapper/wrapper-org-service';

@Component({
  selector: 'app-manage-organisation-profile-site-delete',
  templateUrl: './manage-organisation-profile-site-delete.component.html',
  styleUrls: ['./manage-organisation-profile-site-delete.component.scss'],
  animations: [
      slideAnimation({
          close: { 'transform': 'translateX(12.5rem)' },
          open: { left: '-12.5rem' }
      })
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageOrganisationSiteDeleteComponent extends BaseComponent implements OnInit {

  public item$!: Observable<any>;
  public id: number;
  public routeParams!: any;

  constructor(private ciiService: ciiService, private wrapperService: WrapperUserService, private wrapperOrgService: WrapperOrganisationService, private dataService: dataService, private router: Router, private route: ActivatedRoute, protected uiStore: Store<UIState>, private readonly tokenService: TokenService) {
    super(uiStore);
    this.id = parseInt(this.route.snapshot.paramMap.get('id') || '0');
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.routeParams = params;
    });
  }

  public onSubmit() {
    const accesstoken = this.tokenService.getDecodedAccessToken();
    this.wrapperOrgService.deleteSite(accesstoken.ciiOrgId, this.id).subscribe(data => {
      this.router.navigateByUrl(`manage-org/profile/site/delete/success`);
    });
    // this.router.navigateByUrl('manage-org/profile/site/delete/success');
  }

  public goBack() {
    this.router.navigateByUrl(`manage-org/profile/site/edit/${this.id}`);
  }

}

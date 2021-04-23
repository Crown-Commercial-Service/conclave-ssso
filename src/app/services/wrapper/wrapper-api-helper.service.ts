import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { UserListResponse } from 'src/app/models/user';
import { environment } from 'src/environments/environment';
import { ajax } from 'rxjs/ajax';
import { TokenService } from '../auth/token.service';

@Injectable({
  providedIn: 'root'
})
export class WrapperApiHelperService {

  constructor(private http: HttpClient, private tokenService: TokenService) {
  }

  // public setAccessTokenHeader(options: any){
  //   let accessToken = this.tokenService.getAccessToken();
  //   options.headers = options.headers.set("Authorize", `Bearer ${accessToken}`);
  // }

}
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
// import { Observable } from 'rxjs/internal/Observable';
import 'rxjs/add/observable/of';
import { Observable, Subject } from 'rxjs';
import * as CryptoJS from 'crypto-js';
import { environment } from '../../../environments/environment';
import { ajax } from 'rxjs/ajax';
import { PasswordChangeDetail } from 'src/app/models/passwordChangeDetail';
import { TokenInfo } from 'src/app/models/auth';
import { TokenService } from './token.service';
import { getLocaleDateTimeFormat } from '@angular/common';

@Injectable()
export class AuthService {

  public url: string = environment.uri.api.security;
  public authTokenRenewaltimerReference: any = undefined;
  constructor(private readonly httpService: HttpClient, private readonly tokenService: TokenService) {
  }

  login(username: string, password: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    }
    //ccs-sso-reliable-toucan-ab.london.cloudapps.digital
    const body = { userName: username, userPassword: password }
    return this.httpService.post(`${this.url}/security/login`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  public isUserAuthenticated(): boolean {
    const tokens = localStorage.getItem('access_token');
    return tokens != null;
    // return this.httpService.get<boolean>('/auth/isAuthenticated');
  }

  public registerTokenRenewal() {
    if (this.authTokenRenewaltimerReference == undefined) {
      let thisVar = this;
      this.authTokenRenewaltimerReference = setInterval(function () {
        let accessToken = thisVar.tokenService.getDecodedAccessToken();
        if (accessToken != null) {
          let expireDate = new Date(accessToken.exp * 1000);
          var date = new Date();
          let diffInMinutes = Math.floor((expireDate.getTime() - date.getTime()) / 60000);

          // If token expiration is less than 10 minutes, trigger token renewal 
          if (diffInMinutes <= 10) {
            thisVar.renewAccessToken();
          }
        }
        else {
          thisVar.renewAccessToken();
        }
      }, 300000); // execute every 5 minutes (60000*5)
    }
  }

  private renewAccessToken() {
    this.getRefreshToken().toPromise().then((refreshToken: any) => {
      this.renewToken(refreshToken || '').toPromise().then((tokenInfo: TokenInfo) => {
        this.saveRefreshToken(tokenInfo.refreshToken).toPromise().then(() => {
          localStorage.setItem('access_token', tokenInfo.accessToken);
        });
      },
        (err) => {
          // This could due to invalid refresh token (refresh token rotation)  
          if (err.error == "INVALID_CREDENTIALS") {
            // sign out the user
            this.logOutAndRedirect();
          }
        });
    });
  }

  private authSuccessSource = new Subject<boolean>();

  // Observable string streams
  userAutnenticated$ = this.authSuccessSource.asObservable();

  publishAuthStatus(authSuccess: boolean) {
    this.authSuccessSource.next(authSuccess);
  }

  public isAuthenticated(): Observable<boolean> {
    const tokens = localStorage.getItem('access_token');
    if (tokens) {
      return Observable.of(true);
    }
    return Observable.of(false);
  }

  register(firstName: string, lastName: string, username: string, email: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
        .append("X-API-Key", environment.securityApiKey)
    }
    const body = { FirstName: firstName, LastName: lastName, UserName: username, Email: email, Role: 'Admin', Groups: [] }
    return this.httpService.post(`${this.url}/security/register`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  getAccesstoken() {
    return localStorage.getItem('access_token');
  }

  changePassword(passwordChangeDetail: PasswordChangeDetail): Observable<any> {
    return this.httpService.post(`${environment.uri.api.postgres}/auth/change_password`, passwordChangeDetail).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  resetPassword(userName: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    }
    return this.httpService.post(`${this.url}/security/passwordresetrequest`, "\"" + userName + "\"", options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return "";
      })
    );
  }

  token(code: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    }
    const body = {
      code: code,
      grant_type: 'authorization_code',
      client_id: environment.idam_client_id,
      redirect_uri: environment.uri.web.dashboard + '/authsuccess',
      code_verifier: this.getCodeVerifier()
    };
    return this.httpService.post(`${this.url}/security/token`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  renewToken(refreshToken: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    }
    const body = {
      refresh_token: refreshToken,
      grant_type: 'refresh_token',
      client_id: environment.idam_client_id,
      redirect_uri: environment.uri.web.dashboard + '/authsuccess',
    };

    return this.httpService.post(`${this.url}/security/token`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  saveRefreshToken(refreshToken: string) {
    let coreDataUrl: string = `${environment.uri.api.postgres}/auth/save_refresh_token`;
    const body = {
      'refreshToken': refreshToken
    }
    return this.httpService.post(coreDataUrl, body).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  getRefreshToken() {
    const options = {
      headers: new HttpHeaders().append('responseType', 'text')
    }
    let coreDataUrl: string = `${environment.uri.api.postgres}/auth/get_refresh_token`;
    return this.httpService.get(coreDataUrl, { responseType: 'text' });
  }

  getSignOutEndpoint() {
    return environment.uri.api.security + '/security/logout?clientId=' + environment.idam_client_id
      + '&redirecturi=' + environment.uri.web.dashboard;
  }

  getAuthorizedEndpoint() {
    let codeVerifier = this.getCodeVerifier();
    const codeVerifierHash = CryptoJS.SHA256(codeVerifier).toString(CryptoJS.enc.Base64);
    const codeChallenge = codeVerifierHash
      .replace(/=/g, '')
      .replace(/\+/g, '-')
      .replace(/\//g, '_');
    let url = environment.uri.api.security + '/security/authorize?scope=email profile openid offline_access&response_type=code&client_id='
      + environment.idam_client_id
      + '&code_challenge_method=S256' + '&code_challenge=' + codeChallenge
      + '&redirect_uri=' + environment.uri.web.dashboard + '/authsuccess'

    return url;
  }

  getCodeVerifier() {
    let codeVerifier = localStorage.getItem('codeVerifier');
    if (codeVerifier == undefined || codeVerifier == '') {
      codeVerifier = this.generateRandom(128);
      localStorage.setItem('codeVerifier', codeVerifier);
    }
    return codeVerifier;
  }

  generateRandom(length: number) {
    var result = '';
    var characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    var charactersLength = characters.length;
    for (var i = 0; i < length; i++) {
      result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }
    return result;
  }


  public signOut() {
    clearTimeout(this.authTokenRenewaltimerReference);
    localStorage.removeItem('brickedon_user');
    localStorage.removeItem('user_name');
    localStorage.removeItem('ccs_organisation_id');
    localStorage.removeItem('cii_organisation');
    localStorage.removeItem('brickendon_org_reg_email_address');
    localStorage.removeItem('codeVerifier');
    localStorage.removeItem('securityapiurl');
    localStorage.removeItem('redirect_uri');
    localStorage.removeItem('client_id');
    localStorage.removeItem('access_token');
  }

  public logOut(userName: string | null): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    };
    return this.httpService.post(`${this.url}/security/logout?userName=${userName}`, null, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  public logOutAndRedirect() {
    this.clearRefreshToken().toPromise().then(() => {
      this.signOut();
      window.location.href = this.getSignOutEndpoint();
    }),
      catchError(error => {
        return throwError(error);
      });
  }

  clearRefreshToken() {
    let coreDataUrl: string = `${environment.uri.api.postgres}/auth/sign_out`;
    return this.httpService.post(coreDataUrl, null);
  }

  public setWindowLocationHref(href: string) {
    window.location.href = href;
  }

  getPermissions(): Observable<any> {
    return this.httpService.get(`${environment.uri.api.postgres}/user/GetPermissions`).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }

  nominate(firstName: string, lastName: string, email: string): Observable<any> {
    const token = this.getAccesstoken()+'';
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
        .append("X-API-Key", environment.securityApiKey)
        // .append('Authorization', 'Bearer ' + token)
    }
    const body = { FirstName: firstName, LastName: lastName, UserName: email, Email: email, Role: 'Admin', Groups: [] }
    return this.httpService.post(`${this.url}/security/nominate`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }
}

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

@Injectable()
export class AuthService {

  public url: string = environment.uri.api.security;

  constructor(private readonly httpService: HttpClient) {
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
    const tokens = localStorage.getItem('brickedon_aws_tokens');
    return tokens != null;
    // return this.httpService.get<boolean>('/auth/isAuthenticated');
  }

  private authSuccessSource = new Subject<boolean>();

  // Observable string streams
  userAutnenticated$ = this.authSuccessSource.asObservable();

  publishAuthStatus(authSuccess: boolean) {
    this.authSuccessSource.next(authSuccess);
  }

  public isAuthenticated(): Observable<boolean> {
    const tokens = localStorage.getItem('brickedon_aws_tokens');
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

  changePassword(passwordChangeDetail: PasswordChangeDetail): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
      .append("X-API-Key", environment.securityApiKey)
    }

    return this.httpService.post(`${this.url}/security/changepassword`, passwordChangeDetail, options).pipe(
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
      .append("X-API-Key", environment.securityApiKey)
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
      code_verifier:this.getCodeVarifier()
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

  getSignOutEndpoint() {
    return environment.uri.api.security + '/security/logout?clientId=' + environment.idam_client_id
      + '&redirecturi=' + environment.uri.web.dashboard;
  }

  getAuthorizedEndpoint() {
    let codeVerifier = this.getCodeVarifier();
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

  getCodeVarifier() {
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
    localStorage.removeItem('brickedon_aws_tokens');
    localStorage.removeItem('brickedon_user');
    localStorage.removeItem('user_name');
    localStorage.removeItem('ccs_organisation_id');
    localStorage.removeItem('cii_organisation');
    localStorage.removeItem('brickendon_org_reg_email_address');
    localStorage.removeItem('codeVerifier');
    localStorage.removeItem('securityapiurl');
    localStorage.removeItem('redirect_uri');
    localStorage.removeItem('client_id');
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
    this.signOut();
    window.location.href = this.getSignOutEndpoint();
  }

  public setWindowLocationHref(href: string) {
    window.location.href = href;
  }

  getPermissions(token: string): Observable<any> {
    const options = {
      headers: new HttpHeaders().append('Content-Type', 'application/json')
    }
    const body = {
      //token
    };
    // return this.httpService.post(`${environment.uri.api.postgres}/user/GetPermissions?token=${token}`, body, options).pipe(
    return this.httpService.post(`${environment.uri.api.postgres}/user/GetPermissions?token=123456789`, body, options).pipe(
      map(data => {
        return data;
      }),
      catchError(error => {
        return throwError(error);
      })
    );
  }
}

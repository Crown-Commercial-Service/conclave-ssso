import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { User, UserProfileRequestInfo } from 'src/app/models/user';
import { environment } from 'src/environments/environment';
import { ContactReason } from 'src/app/models/contactDetail';
import { IdentityProvider } from 'src/app/models/identityProvider';
import { WrapperApiHelperService } from './wrapper-api-helper.service';

@Injectable({
  providedIn: 'root'
})
export class WrapperConfigurationService {
  public url: string = `${environment.uri.api.wrapper}/configurations`;

  private options = {
    headers: new HttpHeaders()
  }

  constructor(private http: HttpClient, private wrapperApiService: WrapperApiHelperService) {
  }

  getContactReasons(): Observable<any> {
    const url = `${this.url}/contact-reasons`;
    return this.http.get<ContactReason[]>(url, this.options).pipe(
      map((data: ContactReason[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getIdentityProviders(): Observable<any> {
    const url = `${this.url}/identity-providers`;
    return this.http.get<IdentityProvider[]>(url, this.options).pipe(
      map((data: IdentityProvider[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getRoles(): Observable<any> {
    const url = `${this.url}/roles`;
    return this.http.get<any[]>(url, this.options).pipe(
      map((data: any[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

}
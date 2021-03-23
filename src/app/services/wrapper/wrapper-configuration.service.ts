import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { User, UserProfileRequestInfo } from 'src/app/models/user';
import { environment } from 'src/environments/environment';
import { ContactReason } from 'src/app/models/contactDetail';
import { IdentityProvider } from 'src/app/models/identityProvider';

@Injectable({
  providedIn: 'root'
})
export class WrapperConfigurationService {
  public url: string = `${environment.uri.api.wrapper}/configurations`;

  private options = {
    headers: new HttpHeaders()
  }

  constructor(private http: HttpClient) {
    this.options.headers = this.options.headers.set("X-API-Key", environment.wrapperApiKey);
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

}
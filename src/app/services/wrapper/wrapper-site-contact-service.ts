import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { ContactInfo, SiteContactInfo, SiteContactInfoList } from 'src/app/models/contactInfo';

@Injectable({
  providedIn: 'root'
})
export class WrapperSiteContactService {
  url: string = `${environment.uri.api.wrapper}/organisations`;

  private options = {
    headers: new HttpHeaders(),

  }

  constructor(private http: HttpClient) {
  }

  createSiteContact(organisationId: string, siteId: number, contactInfo: ContactInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}/contacts`;
    return this.http.post<number>(url, contactInfo, this.options).pipe(
      map((data: number) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  deleteSiteContact(organisationId: string, siteId: number, contactId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}/contacts/${contactId}`;
    return this.http.delete(url, this.options).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getSiteContacts(organisationId: string,  siteId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}/contacts`
    return this.http.get<SiteContactInfoList>(url, this.options).pipe(
      map((data: SiteContactInfoList) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getSiteContactById(organisationId: string, siteId: number, contactId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}/contacts/${contactId}`;
    return this.http.get<SiteContactInfo>(url, this.options).pipe(
      map((data: SiteContactInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  updateSiteContact(organisationId: string,  siteId: number, contactId: number, contactInfo: ContactInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}/contacts/${contactId}`;
    return this.http.put(url, contactInfo, this.options).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

}
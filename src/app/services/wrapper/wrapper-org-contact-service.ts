import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { ContactInfo, OrganisationContactInfo, OrganisationContactInfoList } from 'src/app/models/contactInfo';

@Injectable({
  providedIn: 'root'
})
export class WrapperOrganisationContactService {
  url: string = `${environment.uri.api.wrapper}/organisations`;

  private options = {
    headers: new HttpHeaders(),

  }

  constructor(private http: HttpClient) {
  }

  createOrganisationContact(organisationId: string, contactInfo: ContactInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/contacts`;
    return this.http.post<number>(url, contactInfo, this.options).pipe(
      map((data: number) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  deleteOrganisationContact(organisationId: string, contactId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/contacts/${contactId}`;
    return this.http.delete(url, this.options).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationContacts(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/contacts`
    return this.http.get<OrganisationContactInfoList>(url, this.options).pipe(
      map((data: OrganisationContactInfoList) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationContactById(organisationId: string, contactId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/contacts/${contactId}`;
    return this.http.get<OrganisationContactInfo>(url, this.options).pipe(
      map((data: OrganisationContactInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  updateOrganisationContact(organisationId: string, contactId: number, contactInfo: ContactInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/contacts/${contactId}`;
    return this.http.put(url, contactInfo, this.options).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

}
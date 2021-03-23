import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { UserListResponse } from 'src/app/models/user';
import { environment } from 'src/environments/environment';
import { ajax } from 'rxjs/ajax';

@Injectable({
  providedIn: 'root'
})
export class WrapperOrganisationService {
  public url: string = `${environment.uri.api.wrapper}/organisations`;

  private options = {
    headers: new HttpHeaders()
  }

  constructor(private http: HttpClient) {
    this.options.headers = this.options.headers.set("X-API-Key", environment.wrapperApiKey);
  }

  getUsers(organisationId: string, userName: string, currentPage: number, pageSize:number): Observable<any> {
    pageSize = pageSize <=0 ? 10 : pageSize;
    const url = `${this.url}/${organisationId}/user?currentPage=${currentPage}&pageSize=${pageSize}&userName=${userName}`;
    return this.http.get<UserListResponse>(url, this.options).pipe(
      map((data: UserListResponse) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getSites(ciiOrgId: string): Observable<any> {
    const url = `${this.url}/${ciiOrgId}/site`;
    return this.http.get<any>(url, this.options).pipe(
      map((data: any) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getSite(ciiOrgId: string, id: number): Observable<any> {
    const url = `${this.url}/${ciiOrgId}/site/${id}`;
    return this.http.get<any>(url, this.options).pipe(
      map((data: any) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  addSite(ciiOrgId: string, json: string | null): Observable<any> {
    const body = JSON.parse(json+'');
    return ajax({
      url: `${this.url}/${ciiOrgId}/site`,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': environment.wrapperApiKey,
      },
      body:  body
    });
  }

  updateSite(ciiOrgId: string, id: number, json: string | null): Observable<any> {
    const body = JSON.parse(json+'');
    return ajax({
      url: `${this.url}/${ciiOrgId}/site/${id}`,
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': environment.wrapperApiKey,
      },
      body:  body
    });
  }

  deleteSite(ciiOrgId: string, id: number): Observable<any> {
    return ajax({
      url: `${this.url}/${ciiOrgId}/site/${id}`,
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': environment.wrapperApiKey,
      },
    });
  }

}
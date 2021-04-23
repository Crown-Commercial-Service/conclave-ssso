import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { OrganisationSiteInfo, OrganisationSiteInfoList, OrganisationSiteResponse } from 'src/app/models/site';

@Injectable({
  providedIn: 'root'
})
export class WrapperOrganisationSiteService {
  public url: string = `${environment.uri.api.wrapper}/organisations`;

  constructor(private http: HttpClient) {
  }

  createOrganisationSite(organisationId: string, orgSiteInfo: OrganisationSiteInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/sites`;
    return this.http.post<number>(url, orgSiteInfo).pipe(
      map((data: number) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationSites(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/sites`;
    return this.http.get<OrganisationSiteInfoList>(url).pipe(
      map((data: OrganisationSiteInfoList) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationSite(organisationId: string, siteId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}`;
    return this.http.get<OrganisationSiteResponse>(url).pipe(
      map((data: OrganisationSiteResponse) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  updateOrganisationSite(organisationId: string, siteId: number, orgSiteInfo: OrganisationSiteInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}`;
    return this.http.put(url, orgSiteInfo).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  deleteOrganisationSite(organisationId: string, siteId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/sites/${siteId}`;
    return this.http.delete(url).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }
}
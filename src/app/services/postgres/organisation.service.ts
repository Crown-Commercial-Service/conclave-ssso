import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { Organisation } from 'src/app/models/organisation';

@Injectable({
    providedIn: 'root'
})
export class OrganisationService {
    public url: string =  `${environment.uri.api.postgres}/organisation`;

    constructor(private http: HttpClient) {}

    get(id:number): Observable<any> {        
        const url = `${this.url}/${id}`;
        var user = this.http.get<any>(url).pipe(
            map((data: any) => {
              return data;
            }), catchError(error => {
              console.log(error);
              return throwError(error);
            })
         )
        return user;
    }

    add(organisation: any): Observable<any> {
      const options = {
        headers: new HttpHeaders().append('Content-Type', 'application/json')
      }
      const body = { legalName: organisation.identifier.legalName, ciiOrganisationId: organisation.ccsOrgId+'', contactPoint: organisation.contactPoint, address: organisation.address, organisationUri: organisation.identifier.uri, rightToBuy: true }
      return this.http.post(`${this.url}`, body, options).pipe(
        map(data => {
          return data;
        }),
        catchError(error => {
          console.log(error);
          return throwError(error);
        })
      );
    }
}
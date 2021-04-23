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

    get(): Observable<any> {        
        const url = `${this.url}/getAll`;
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

    getById(id:number): Observable<any> {        
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

    getUsers(): Observable<any> {        
      const url = `${this.url}/getUsers`;
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
      const body = { legalName: organisation.identifier.legalName, ciiOrganisationId: organisation.ccsOrgId+'', contactPoint: organisation.contactPoint, address: organisation.address, organisationUri: organisation.identifier.uri, rightToBuy: organisation.rightToBuy, supplierBuyerType: organisation.supplierBuyerType, businessType: organisation.buyerType  }
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

    put(organisation: any): void {
      const options = {
        headers: new HttpHeaders().append('Content-Type', 'application/json')
      }
      const body = { legalName: organisation.identifier.legalName, ciiOrganisationId: organisation.ccsOrgId+'', contactPoint: organisation.contactPoint, address: organisation.address, organisationUri: organisation.identifier.uri, rightToBuy: organisation.rightToBuy, supplierBuyerType: organisation.supplierBuyerType, businessType: organisation.buyerType }
      this.http.put(`${this.url}`, body, options).pipe(
        map(data => {
          console.log(data);
        })
      );
    }

    rollback(model: any): Observable<any> {
      const options = {
        headers: new HttpHeaders().append('Content-Type', 'application/json')
      }
      const body = { organisationId: model.organisationId, ciiOrganisationId: model.ciiOrganisationId, physicalAddressId: model.physicalAddressId }
      return this.http.post(`${this.url}/rollback`, body, options).pipe(
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
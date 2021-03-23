import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
//import { Observable, catchError, throwError, map } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { User } from 'src/app/models/user';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class UserService {
    public url: string =  `${environment.uri.api.postgres}/user`;

    constructor(private http: HttpClient) {}

    getUser(userName:string): Observable<any> {        
      const url = `${this.url}/getuser?userName=${userName}`;
      return this.http.get<User>(url).pipe(
        map((data: User) => {
          return data;
        }), catchError( error => {
          return throwError(error);
        })
      );
    }

    add(model: any): Observable<any> {
      const options = {
        headers: new HttpHeaders().append('Content-Type', 'application/json')
      }
      const body = { firstName: model.firstName, lastName: model.lastName, userName: model.userName, partyId: 1, title: 1, jobTitle: model.title, organisationId: model.orgId }
      return this.http.post(`${this.url}`, body, options).pipe(
        map(data => {
          return data;
        }),
        catchError(error => {
          return throwError(error);
        })
      );
    }
}
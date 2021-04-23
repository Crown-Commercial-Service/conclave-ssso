import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { User, UserEditResponseInfo, UserProfileRequestInfo, UserProfileResponseInfo } from 'src/app/models/user';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WrapperUserService {
  public userEndpointUrl: string = `${environment.uri.api.wrapper}/users`;
  public orgUserEndpointUrl: string = `${environment.uri.api.wrapper}/users`;

  private options = {
    headers: new HttpHeaders()
  }

  constructor(private http: HttpClient) {
  }

  createUser(userRequest: UserProfileRequestInfo): Observable<any> {
    const url = `${this.userEndpointUrl}`;
    return this.http.post<UserEditResponseInfo>(url, userRequest, this.options).pipe(
      map((data : UserEditResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  deleteUser(userName: string): Observable<any> {
    const url = `${this.userEndpointUrl}?userId=${userName}`;
    return this.http.delete(url, this.options).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getUser(userName: string): Observable<any> {
    const url = `${this.userEndpointUrl}?userId=${userName}`;
    return this.http.get<UserProfileResponseInfo>(url, this.options).pipe(
      map((data: UserProfileResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  updateUser(userName: string, userRequest: UserProfileRequestInfo): Observable<any> {
    const url = `${this.userEndpointUrl}?userId=${userName}`;
    return this.http.put<UserEditResponseInfo>(url, userRequest, this.options).pipe(
      map((data: UserEditResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  updateUserRoles(userName: string, userRequest: UserProfileRequestInfo): Observable<any> {
    const url = `${this.userEndpointUrl}/UpdateUserRoles?userId=${userName}`;
    return this.http.put<UserEditResponseInfo>(url, userRequest, this.options).pipe(
      map((data: UserEditResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  addAdminRole(userName: string, userRequest: UserProfileRequestInfo): Observable<any> {
    const url = `${this.userEndpointUrl}/AddAdminRole?userId=${userName}`;
    return this.http.put<UserEditResponseInfo>(url, userRequest, this.options).pipe(
      map((data: UserEditResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

}
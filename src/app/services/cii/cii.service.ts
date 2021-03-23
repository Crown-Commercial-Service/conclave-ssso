import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Data } from '../../models/data';
import { identityService } from '../identity/identity.service';
import { JwtToken } from '../../models/jwtToken';
import { from, Observable, of, throwError } from 'rxjs';
import { switchMap, catchError, map } from 'rxjs/operators';
import { Scheme } from '../../models/scheme';
import { fromFetch } from 'rxjs/fetch'
import { ajax } from 'rxjs/ajax';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ciiService {

  public url: string = environment.uri.api.postgres;

  constructor(private http: HttpClient, private identityService: identityService) { }

  getSchemes(): Observable<any> {
    return fromFetch(`${this.url}/cii/GetSchemes`, {
      headers: {
        'Content-Type': 'application/json',
      },
      method: 'GET'
    }).pipe(
      switchMap(response => {
        if (response.ok) {
          return response.json();
        } else {
          return of({ error: true, message: `Error ${response.status}` });
        }
      }),
      catchError(err => {
        console.error(err);
        return of({ error: true, message: err.message })
      })
    );
  }

  getDetails(scheme: string, id: string): Observable<any> {
    return fromFetch(`${this.url}/cii/${scheme}?&companyNumber=${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      method: 'GET'
    }).pipe(
      switchMap(response => {
        if (response.ok) {
          return response.json();
        } else {
          return of({ error: true, message: `Error ${response.status}` });
        }
      }),
      catchError(err => {
        console.error(err);
        return of({ error: true, message: err.message })
      })
    );
  }

  getOrg(scheme: string, id: string): Observable<any> {
    return fromFetch(`${this.url}/cii/GetOrg?id=${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      method: 'GET'
    }).pipe(
      switchMap(response => {
        if (response.ok) {
          return response.json();
        } else {
          return of({ error: true, message: `Error ${response.status}` });
        }
      }),
      catchError(err => {
        console.error(err);
        return of({ error: true, message: err.message })
      })
    );
  }

  getOrgs(id: string): Observable<any> {
    return fromFetch(`${this.url}/cii/GetOrgs?id=${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      method: 'GET'
    }).pipe(
      switchMap(response => {
        if (response.ok) {
          return response.json();
        } else {
          return of({ error: true, message: `Error ${response.status}` });
        }
      }),
      catchError(err => {
        console.error(err);
        return of({ error: true, message: err.message })
      })
    );
  }

  getIdentifiers(orgId: string, scheme: string, id: string): Observable<any> {
    return fromFetch(`${this.url}/cii/GetIdentifiers/?orgId=${orgId}&scheme=${scheme}&id=${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      method: 'GET'
    }).pipe(
      switchMap(response => {
        if (response.ok) {
          return response.json();
        } else {
          return of({ error: true, message: `Error ${response.status}` });
        }
      }),
      catchError(err => {
        console.error(err);
        return of({ error: true, message: err.message })
      })
    );
  }

  addOrganisation(json: string | null): Observable<any> {
    const body = JSON.parse(json+'');
    return ajax({
      url: `${this.url}/cii/`,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body:  body
    });
  }

  updateOrganisation(json: string | null): Observable<any> {
    const body = JSON.parse(json+'');
    return ajax({
      url: `${this.url}/cii/`,
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body:  body
    });
  }

  delete(json: string | null): Observable<any> {
    const body = JSON.parse(json+'');
    return ajax({
      url: `${this.url}/cii/`,
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
      body:  body
    });
  }

  deleteById(id: string): Observable<any> {
    return ajax({
      url: `${this.url}/cii/?id=` + id,
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  deleteOrganisation(id: string): Observable<any> {
    return ajax({
      url: `${this.url}/cii/DeleteOrg?id=` + id,
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  deleteScheme(orgId: string, scheme: string, id: string): Observable<any> {
    return ajax({
      url: `${this.url}/cii/DeleteScheme?orgId=${orgId}&scheme=${scheme}&id=${id}`,
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

}

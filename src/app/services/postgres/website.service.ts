import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Website } from 'src/app/models/website';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class WebsiteService {
  url: string = `${environment.uri.api.postgres}/website`;

    constructor(private http: HttpClient) { }

    public get(organisationId: number) {
        return this.http.get<Website[]>(this.url, {
            headers: { Authorization: `Bearer ${"data.token"}` },
            params: { "organisationId": organisationId.toString() }
        });
    }

    public getById(id: number) {
        return this.http.get<Website>(`${this.url}/${id}`, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }

    public post(contact: Website) {
        const options = {
          headers: new HttpHeaders()
          .append('Content-Type', 'application/json')
        }
        return this.http.post(`${this.url}`, contact, options).pipe(
          map(data => {
            return data;
          }),
          catchError(error => {
            console.log(error);
            return throwError(error);
          })
        );
    }

    public put(id: number, contact: Website) {
        return this.http.put<number>(`${this.url}/${id}`, contact, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }

    public delete(id: number) {
        return this.http.delete(`${this.url}/${id}`, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }
}

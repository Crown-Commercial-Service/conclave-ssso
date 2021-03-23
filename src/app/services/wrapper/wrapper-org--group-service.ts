import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { Group } from 'src/app/models/organisationGroup';

@Injectable({
  providedIn: 'root'
})
export class WrapperOrganisationGroupService {
  public url: string = `${environment.uri.api.wrapper}/organisations`;

  private options = {
    headers: new HttpHeaders()
  }

  constructor(private http: HttpClient) {
    this.options.headers = this.options.headers.set("X-API-Key", environment.wrapperApiKey);
  }

  getOrganisationGroups(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/groups`;
    return this.http.get<Group[]>(url, this.options).pipe(
      map((data: Group[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

}
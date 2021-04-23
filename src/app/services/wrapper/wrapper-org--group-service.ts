import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

import { environment } from 'src/environments/environment';
import { Group, GroupList, OrganisationGroupNameInfo, OrganisationGroupRequestInfo, OrganisationGroupResponseInfo, Role } from 'src/app/models/organisationGroup';
import { IdentityProvider } from 'src/app/models/identityProvider';
import { ajax } from 'rxjs/ajax';

@Injectable({
  providedIn: 'root'
})
export class WrapperOrganisationGroupService {
  public url: string = `${environment.uri.api.wrapper}/organisations`;

  constructor(private http: HttpClient) {
  }

  createOrganisationGroups(organisationId: string, orgGroupNameInfo: OrganisationGroupNameInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/groups`;
    return this.http.post<number>(url, orgGroupNameInfo).pipe(
      map((data: number) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationGroups(organisationId: string, searchString: string= ''): Observable<any> {
    const url = `${this.url}/${organisationId}/groups?searchString=${searchString}`;
    return this.http.get<GroupList>(url).pipe(
      map((data: GroupList) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationGroup(organisationId: string, groupId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/groups/${groupId}`;
    return this.http.get<OrganisationGroupResponseInfo>(url).pipe(
      map((data: OrganisationGroupResponseInfo) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  patchUpdateOrganisationGroup(organisationId: string, groupId: number, orgGroupPatchInfo: OrganisationGroupRequestInfo): Observable<any> {
    const url = `${this.url}/${organisationId}/groups/${groupId}`;
    return this.http.patch(url, orgGroupPatchInfo).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  deleteOrganisationGroup(organisationId: string, groupId: number): Observable<any> {
    const url = `${this.url}/${organisationId}/groups/${groupId}`;
    return this.http.delete(url).pipe(
      map(() => {
        return true;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationRoles(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/roles`;
    return this.http.get<Role[]>(url).pipe(
      map((data: Role[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getEligableRoles(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/eligableRoles`;
    return this.http.get<Role[]>(url).pipe(
      map((data: Role[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  getOrganisationIdentityProviders(organisationId: string): Observable<any> {
    const url = `${this.url}/${organisationId}/identity-providers`;
    return this.http.get<IdentityProvider[]>(url).pipe(
      map((data: IdentityProvider[]) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }

  enableIdentityProvider(organisationId: string, name: string, enabled: boolean): Observable<any> {
    return this.http.put<any>(`${this.url}/${organisationId}/identity-providers/update?idpName=${name}&enabled=${enabled}`, null).pipe(
      map((data: any) => {
        return data;
      }), catchError(error => {
        return throwError(error);
      })
    );
  }
}
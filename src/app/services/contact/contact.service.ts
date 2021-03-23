import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { identityService } from '../identity/identity.service';
import { JwtToken } from '../../models/jwtToken';
import { ContactDetails } from 'src/app/models/contactDetail';
import { environment } from '../../../environments/environment';

import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class contactService {
  contactUrl: string = `${environment.uri.api.postgres}/contact`;

    constructor(private http: HttpClient, private identityService: identityService) { }

    // TODO: Once the token is provided by the identity server we should change this to get the token from the store instead of calling the identityService service
    public getContacts(organisationId: number) {
        // const data:JwtToken = await this.identityService.getToken().toPromise();
        return this.http.get<ContactDetails[]>(this.contactUrl, {
            headers: { Authorization: `Bearer ${"data.token"}` },
            params: { "organisationId": organisationId.toString() }
        });
    }

    public getContact(contactId: number) {
        // const data:JwtToken = await this.identityService.getToken().toPromise();
        return this.http.get<ContactDetails>(`${this.contactUrl}/${contactId.toString()}`, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }

    public createContact(contact: ContactDetails) {
        return this.http.post<number>(this.contactUrl, contact, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }

    public createContactV2(contact: ContactDetails) {
        const options = {
          headers: new HttpHeaders()
          .append('Content-Type', 'application/json')
        }
        return this.http.post(`${this.contactUrl}`, contact, options).pipe(
          map(data => {
            return data;
          }),
          catchError(error => {
            console.log(error);
            return throwError(error);
          })
        );
    }

    public updateContact(contactId: number, contact: ContactDetails) {
        return this.http.put<number>(`${this.contactUrl}/${contactId.toString()}`, contact, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }

    public deleteContact(contactId: number) {
        return this.http.delete(`${this.contactUrl}/${contactId.toString()}`, {
            headers: { Authorization: `Bearer ${"data.token"}` }
        });
    }
}

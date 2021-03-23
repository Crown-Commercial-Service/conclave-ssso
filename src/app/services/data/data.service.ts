import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Data } from '../../models/data';
import { identityService } from '../identity/identity.service';
import { JwtToken } from '../../models/jwtToken';

@Injectable({
    providedIn: 'root'
})
export class dataService {
    dataUrl: string = 'http://localhost:44352/api/data';

    constructor(private http: HttpClient, private identityService: identityService) {}

    // TODO: Once the token is provided by the identity server we should change this to get the token from the store instead of calling the identityService service
    async getData() {
        const data:JwtToken = await this.identityService.getToken().toPromise();
        return this.http.get<Data[]>(this.dataUrl, { headers: { Authorization: `Bearer ${data.token}`}});
    }
}
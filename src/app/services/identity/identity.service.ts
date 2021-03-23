import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { JwtToken } from '../../models/jwtToken';

@Injectable({
    providedIn: 'root',
})
export class identityService {
    // identityUrl: string = 'http://localhost:4040/api/v2.0/token';
    identityUrl: string = 'https://localhost:44352/Security/token';
    // jwtToken: JwtToken;

    constructor(private http: HttpClient) {}

    public getToken(): Observable<JwtToken> {
        return this.http.get<JwtToken>(this.identityUrl, { withCredentials: true});
    }
}
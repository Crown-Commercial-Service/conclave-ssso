import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { JwtToken } from '../../models/jwtToken';
import { catchError, map } from 'rxjs/operators';

@Injectable({
    providedIn: 'root',
})
export class JwtAuthService {
    private static readonly URLS: RegExp[] = [new RegExp('http://localhost:44352/api/v2.0/protectedUrls')];
    // private static readonly JWT_AUTH_URL: string = 'http://localhost:4040/api/v2.0/token';
    private static readonly JWT_AUTH_URL: string = 'http://localhost:44352/Security/login';
    private jwtTokenSubject: BehaviorSubject<any>;

    constructor(private http: HttpClient) {
        this.jwtTokenSubject = new BehaviorSubject<any>(null);
    }

    public get jwtToken(): JwtToken {
        return this.jwtTokenSubject.value;
    }

    login(): Observable<JwtToken> {
        return this.http.get<any>(JwtAuthService.JWT_AUTH_URL, {
            observe: 'body' as const,
            responseType: 'json' as const
        }).pipe(
            map(data => {
                const token = new JwtToken(data.token, data.user, new Date(data.vaidUntil));
                this.jwtTokenSubject.next(token);
                return token;
            }),
            catchError(error => {
                return throwError(error);
            })
        );
    }

    refreshToken(): Observable<JwtToken> {
        return this.login();
    }

    authRequired(url:string): boolean {
        return JwtAuthService.URLS.some((regEx: RegExp, currentIndex: number, array: RegExp[]) => {
            return regEx.test(url);
        });
    }

    tokenExpired(): boolean {
        const token = this.jwtTokenSubject.value;
        const timeout = token ? token.validUntil.getTime() - Date.now() - (60 * 1000) : 0;
        return timeout <= 0;
    }
}
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { JwtAuthService } from '../services/jwt/jwt.auth.service';
import { AuthService } from '../services/auth/auth.service';
import { map } from 'rxjs/operators';
import { catchError } from 'rxjs/operators';

@Injectable()
export class HttpJwtAuthInterceptor implements HttpInterceptor {

    constructor(private jwt: JwtAuthService, private authService: AuthService) {

    }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        let token = this.authService.getAccesstoken();
        request = request.clone({
            setHeaders: {
                Authorization: 'Bearer ' + token
            },
            withCredentials: true
        });

        if (!request.headers.has('Content-Type')) {
            request = request.clone({ headers: request.headers.set('Content-Type', 'application/json') });
        }

        return next.handle(request).pipe(
            map((event: HttpEvent<any>) => {
                // if (event instanceof HttpResponse) {
                //     console.log('event--->>>', event);
                // }
                return event;
            }),
            catchError((error: HttpErrorResponse) => {
                if (error.status == 401) {
                    this.authService.logOutAndRedirect();
                }
                return throwError(error);
            }));
    }
}

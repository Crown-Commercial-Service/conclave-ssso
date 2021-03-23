import { HttpEvent,HttpHandler, HttpInterceptor, HttpRequest} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JwtAuthService } from '../services/jwt/jwt.auth.service';
import { mergeMap } from 'rxjs/operators';
import { JwtToken } from '../models/jwtToken';

@Injectable()
export class HttpJwtAuthInterceptor implements HttpInterceptor {

    constructor(private jwt: JwtAuthService) {

    }

    private static appendToken(req: HttpRequest<any>, jwtToken: JwtToken): HttpRequest<any> {
        req = req.clone({
            setHeaders: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            // 'Apikey': 'zhoOHXSyBfUDBqvujje3',
            // Authorization: 'Bearer ' + jwtToken.token
            // 'Access-Control-Allow-Origin': '*',
            },
            withCredentials: true
        });
        return req;
    }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const token = new JwtToken('', '', new Date());
        req = HttpJwtAuthInterceptor.appendToken(req,token);
        return next.handle(req);
        // if (this.jwt.authRequired(req.url)) {
        //     if (this.jwt.tokenExpired()) {
        //         return this.jwt.login().pipe(
        //             mergeMap((value: JwtToken, index: number) => {
        //                 req = HttpJwtAuthInterceptor.appendToken(req,value);
        //                 return next.handle(req);
        //             })
        //         );
        //     } else {
        //         req = HttpJwtAuthInterceptor.appendToken(req, this.jwt.jwtToken);
        //     }
        // }
        // return next.handle(req);
    }
}

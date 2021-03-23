import { HttpEvent,HttpHandler, HttpInterceptor, HttpRequest} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable()
export class HttpBasicAuthInterceptor implements HttpInterceptor {

    constructor() { }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!req.headers.has('Authorization')) {
            req = req.clone({
                setHeaders: {
                    'Accept': 'application/json',
                    Authoriaton: 'Basic' + btoa('cognito:n7u5K9RL')
                },
                withCredentials:true
            });
        }
        return next.handle(req);
    }
}

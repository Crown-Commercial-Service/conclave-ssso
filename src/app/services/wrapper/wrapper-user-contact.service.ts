import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ContactInfo, UserContactInfo, UserContactInfoList } from 'src/app/models/userContact';

@Injectable({
    providedIn: 'root'
})
export class WrapperUserContactService {
    public url: string = `${environment.uri.api.wrapper}/users/contact`;

    private options = {
        headers: new HttpHeaders()
    }

    constructor(private http: HttpClient) {
        this.options.headers = this.options.headers.set("X-API-Key", environment.wrapperApiKey);
    }

    createUserContact(userName: string, contactInfo: ContactInfo): Observable<any> {
        const url = `${this.url}?userId=${userName}`;
        return this.http.post<number>(url, contactInfo, this.options).pipe(
            map((data: number) => {
                return data;
            }), catchError(error => {
                return throwError(error);
            })
        );
    }

    deleteUserContact(userName: string, contactId:number): Observable<any> {
        const url = `${this.url}/${contactId}?userId=${userName}`;
        return this.http.delete(url, this.options).pipe(
            map(() => {
                return true;
            }), catchError(error => {
                return throwError(error);
            })
        );
    }

    getUserContacts(userName: string): Observable<any> {
        const url = `${this.url}?userId=${userName}`;
        return this.http.get<UserContactInfoList>(url, this.options).pipe(
            map((data: UserContactInfoList) => {
                return data;
            }), catchError(error => {
                return throwError(error);
            })
        );
    }

    getUserContactById(userName: string, contactId: number): Observable<any> {
        const url = `${this.url}/${contactId}?userId=${userName}`;
        return this.http.get<UserContactInfo>(url, this.options).pipe(
            map((data: UserContactInfo) => {
                return data;
            }), catchError(error => {
                return throwError(error);
            })
        );
    }

    updateUserContact(userName: string, contactId:number, contactInfo: ContactInfo): Observable<any> {
        const url = `${this.url}/${contactId}?userId=${userName}`;
        return this.http.put(url, contactInfo, this.options).pipe(
            map(() => {
                return true;
            }), catchError(error => {
                return throwError(error);
            })
        );
    }

}
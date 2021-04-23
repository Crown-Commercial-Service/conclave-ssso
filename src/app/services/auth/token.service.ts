import { Injectable } from "@angular/core";
import jwt_decode from 'jwt-decode';

@Injectable()
export class TokenService {
  constructor() {
  }

  getDecodedToken(token: string): any {
    try{
        let jwtToken = jwt_decode(token);
        return jwtToken
    }
    catch(Error){
        return null;
    }
  }

  getDecodedAccessToken(): any {
    let accessToken = this.getDecodedToken(localStorage.getItem('access_token') || '');
    return accessToken;
  }
}
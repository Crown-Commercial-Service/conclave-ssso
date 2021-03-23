import { Injectable } from "@angular/core";
import jwt_decode from 'jwt-decode';

@Injectable()
export class TokenService {
  constructor() {
  }

  getDecodedIdToken(token: string): any {
    try{
        let jwtToken = jwt_decode(token);
        return jwtToken
    }
    catch(Error){
        return null;
    }
  }

  getDecodedAccessToken(): any {
    const tokens = JSON.parse(localStorage.getItem('brickedon_aws_tokens')+'');
    let accessToken = this.getDecodedIdToken(tokens.accessToken);
    return accessToken;
  }
}
export interface TokenInfo
{
    challengeRequired: boolean;
    challengeName: string;
    sessionId : string;
    idToken : string;
    accessToken : string;
    refreshToken : string;
    sessionState:string
}

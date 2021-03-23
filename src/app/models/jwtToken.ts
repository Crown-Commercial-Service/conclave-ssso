class JwtToken {
    // token?: string;
    // validUntil?: string | undefined;
    // user?: string;
    constructor(readonly token?: string, readonly user?: string, readonly validUntil?: Date) {

    }
}
export { JwtToken };
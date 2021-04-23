export class ContactDetails {
    public contactId?: number;
    public partyId?: number;
    public organisationId?: number;
    public name?: string;
    public email?: string;
    public phoneNumber?: string;
    public contactType?: ContactType;
    public address?: Address;
    public teamName?: string;
    public contactReason?: string;
}

export interface Address {
    streetAddress?: string;
    locality?: string;
    region?: string;
    postalCode?: string;
    countryCode?: string;
    uprn?: string;
}

export enum ContactType{
    Organisation,
    OrganisationPerson,
    User
}

export interface ContactReason{
    key: string;
    value: string;
}
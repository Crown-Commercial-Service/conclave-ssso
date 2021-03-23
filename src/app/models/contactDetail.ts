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
}

export class Address{
    public streetAddress?: string;
    public locality?: string;
    public region?: string;
    public postalCode?: string;
    public countryCode?: string;
    public uprn?: string;
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
export interface Organisation {
    organisationId: number;
    organisationUri: string;
    ciiOrganisationId: string;
    legalName: string;
    partyId: number;
    rightToBuy: boolean;
    address?: {
        streetAddress: string;
        locality: string;
        region: string;
        postalCode: string;
        countryCode: string;
        uprn: string;
    },
    contactPoint?: {
        contactId: number;
        partyId: number;
        organisationId: number;
        name: string;
        email: string;
        teamName: string;
        phoneNumber: string;
        fax: string;
        webUrl: string;
        contactType: number;
        contactReason: string;
        address?: {
            streetAddress: string;
            locality: string;
            region: string;
            postalCode: string;
            countryCode: string;
            uprn: string;
        }
    }
}
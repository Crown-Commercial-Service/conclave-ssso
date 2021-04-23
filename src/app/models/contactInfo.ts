export interface ContactInfo
{
    contactId?: number;
    contactReason?: string;
    name?: string;
    email?: string;
    phoneNumber?: string;
    fax?: string;
    webUrl?: string;
}

export interface UserContactInfo extends ContactInfo
{
    userId: string;
    organisationId: string;
}

export interface UserContactInfoList{
    userId: string;
    organisationId: string;
    contactsList: ContactInfo[]
}

export interface OrganisationContactInfo extends ContactInfo
{
    organisationId: string;
}

export interface OrganisationContactInfoList{
    organisationId: string;
    contactsList: ContactInfo[]
}

export interface SiteContactInfo extends ContactInfo
{
    organisationId: string;
    siteId: number;
}

export interface SiteContactInfoList{
    organisationId: string;
    siteId: number;
    siteContacts: ContactInfo[]
}
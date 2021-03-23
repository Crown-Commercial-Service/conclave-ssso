export interface UserContactInfoList{
    userId: string;
    organisationId: string;
    contactsList: ContactInfo[]
}

export interface UserContactInfo extends ContactInfo
{
    userId: string;
    organisationId: string;
}

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
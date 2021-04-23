import { Address } from "./contactDetail";


export interface OrganisationSiteInfo {
    siteName: string;
    address: Address
}

export interface OrganisationSite extends OrganisationSiteInfo {
    details: SiteDetail;
}

export interface OrganisationSiteResponse extends OrganisationSite {
    organisationId: string;
}

export interface OrganisationSiteInfoList {
    organisationId: string;
    sites: OrganisationSite[];
}

export interface SiteGridInfo extends Address {
    siteId: number;
    siteName: string;
}

export interface SiteDetail {
    siteId: number;
}
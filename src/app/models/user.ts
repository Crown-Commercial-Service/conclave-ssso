import { UserTitleEnum } from "../constants/enum";
import { PaginationInfo } from "./paginationInfo";

export interface User {
    id: number,
    userName: string;
    userGroups: UserGroup[]
    organisationId: string;
}

export interface UserGroup {
    group: string;
    accessRole: string;
}

export interface UserProfileRequestInfo {
    organisationId: string;
    userName: string;
    title: UserTitleEnum;
    firstName: string;
    lastName: string;
    groupIds?: number[];
    identityProviderId?: number
}

export interface UserProfileResponseInfo extends UserProfileRequestInfo{
    id: number;
    identityProvider?: string;
    identityProviderDisplayName?: string;
    canChangePassword: boolean;
    userGroups?: UserGroup[];
}

export interface UserListInfo {
    name: string;
    userName: string;
}

export interface UserListResponse extends PaginationInfo {
    organisationId: string;
    userList: UserListInfo[];
}
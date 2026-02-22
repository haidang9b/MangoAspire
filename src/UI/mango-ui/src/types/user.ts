export interface UserInfo {
    sub: string;
    name: string;
    given_name: string;
    family_name: string;
    email: string;
    role?: string | string[];
}

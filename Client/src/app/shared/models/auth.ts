export interface LoginRequest {
    email: string;
    password: string;
    tenantSlug?: string;
}

export interface TenantChoice {
    slug: string;
    name: string;
}

export interface LoginResponse {
    accessToken: string | null;
    accessTokenExpiresAtUtc: string | null;
    tenantSelectionRequired: boolean;
    tenants: TenantChoice[] | null;
}

export interface MeResponse {
    userId: string;
    tenantId: string | null;
    isPlatformAdmin: boolean;
}
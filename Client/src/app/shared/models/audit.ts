export interface FieldChange {
    field: string;
    old: string | null;
    new: string | null;
    hidden: string | null;
}

export interface AuditEntry {
    id: string;
    timestampUtc: string;
    action: string;
    entityName: string;
    entityId: string;
    userId: string | null;
    userName: string;
    ipAddress: string | null;
    changes: FieldChange[];
}

export interface AuditPage {
    items: AuditEntry[];
    total: number;
    page: number;
    pageSize: number;
}

export interface AuditFacets {
    entities: string[];
    actions: string[];
    users: { id: string; name: string}[];
}

export interface AuditFilters {
    entity: string;
    entityId: string;
    action: string;
    userId: string;
    from: string;
    to: string;
}
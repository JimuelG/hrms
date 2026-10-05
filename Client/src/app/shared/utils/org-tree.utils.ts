import { Employee } from "../models/employee";

export interface OrgNode {
    employee: Employee;
    children: OrgNode[];
}

export function buildOrgTree(employees: Employee[]): OrgNode[] {
    const byId = new Map(employees.map((e) => [e.id, e]));
    const childrenByManager = new Map<string, Employee[]>();

    for (const e of employees) {
        const key = e.managerId && byId.has(e.managerId) ? e.managerId : 'root';
        if (!childrenByManager.has(key))
            childrenByManager.set(key, []);

        childrenByManager.get(key)!.push(e);
    }

    const toNode = (e: Employee): OrgNode => ({
        employee: e,
        children: (childrenByManager.get(e.id) ?? []).map(toNode)
    });

    return (childrenByManager.get('root') ?? []).map(toNode); 
}
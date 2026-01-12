export interface Employee {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    document: string;
    roleId: string;
    roleName: string;
    managerName: string;
    rank: number;
    managerId?: string;
    birthDate: string;
    createdAtUtc: string;
    phones: Phone[];
}

export interface Phone {
    number: string;
    type: string;
    isPrimary: boolean;
}

export interface Role {
    id: string;
    name: string;
    rank: number;
}

export interface PagedResult<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
}

export interface EmployeeListQuery {
    page?: number;
    pageSize?: number;
    search?: string;
    orderBy?: string;
    orderDirection?: 'asc' | 'desc';
    firstName?: string;
    lastName?: string;
    email?: string;
    document?: string;
    roleId?: string;
    managerId?: string;
}

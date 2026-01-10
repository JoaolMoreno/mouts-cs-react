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
}

export interface Role {
    id: string;
    name: string;
    rank: number;
}

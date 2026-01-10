'use client';

import { EmployeeForm } from '@/components/EmployeeForm';
import { useEffect, useState } from 'react';
import api from '@/services/api';
import { useParams } from 'next/navigation';
import { Employee } from '@/types';

export default function EditEmployeePage() {
    const { id } = useParams();
    const [employee, setEmployee] = useState<Employee | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchEmployee = async () => {
            try {
                const response = await api.get<Employee>(`/employees/${id}`);
                setEmployee(response.data);
            } catch (error) {
                console.error('Failed to fetch employee', error);
            } finally {
                setLoading(false);
            }
        };

        if (id) fetchEmployee();
    }, [id]);

    if (loading) return <div>Loading...</div>;
    if (!employee) return <div>Employee not found</div>;

    return <EmployeeForm initialData={employee} isEdit />;
}

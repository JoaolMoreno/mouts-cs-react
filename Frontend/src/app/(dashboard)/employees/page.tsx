'use client';

import { useEffect, useState } from 'react';
import api from '@/services/api';
import { Employee } from '@/types';
import Link from 'next/link';
import { Plus, PencilSimple, Trash, Eye } from '@phosphor-icons/react';
import styles from './page.module.scss';

export default function EmployeesPage() {
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const fetchEmployees = async () => {
        try {
            const response = await api.get<Employee[]>('/employees');
            setEmployees(response.data);
        } catch (error) {
            console.error('Failed to fetch employees', error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchEmployees();
    }, []);

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this employee?')) return;
        try {
            await api.delete(`/employees/${id}`);
            setEmployees(prev => prev.filter(e => e.id !== id));
        } catch (error) {
            alert('Failed to delete employee');
        }
    };

    if (isLoading) return <div className={styles.loading}>Loading employees...</div>;

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <h1 className={styles.title}>Employees</h1>
                <Link href="/employees/new" className={styles.createButton}>
                    <Plus size={20} weight="bold" />
                    Add Employee
                </Link>
            </div>

            <div className={styles.tableContainer}>
                <table className={styles.table}>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Role</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {employees.map((employee) => (
                            <tr key={employee.id}>
                                <td>
                                    <div className={styles.nameCell}>
                                        <div className={styles.avatarPlaceholder}>
                                            {employee.firstName[0]}{employee.lastName[0]}
                                        </div>
                                        <div>
                                            <div className={styles.fullName}>{employee.firstName} {employee.lastName}</div>
                                            <div className={styles.document}>{employee.document}</div>
                                        </div>
                                    </div>
                                </td>
                                <td>{employee.email}</td>
                                <td>
                                    <span className={styles.roleBadge}>{employee.roleName}</span>
                                </td>
                                <td>
                                    <div className={styles.actions}>
                                        <Link href={`/employees/${employee.id}`} className={styles.actionButton} title="View Details">
                                            <Eye size={20} />
                                        </Link>
                                        <Link href={`/employees/${employee.id}/edit`} className={styles.actionButton} title="Edit">
                                            <PencilSimple size={20} />
                                        </Link>
                                        <button onClick={() => handleDelete(employee.id)} className={`${styles.actionButton} ${styles.delete}`} title="Delete">
                                            <Trash size={20} />
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                        {employees.length === 0 && (
                            <tr>
                                <td colSpan={4} className={styles.empty}>No employees found.</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

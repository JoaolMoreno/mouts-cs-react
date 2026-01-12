'use client';

import { useEffect, useState } from 'react';
import api from '@/services/api';
import { useParams, useRouter } from 'next/navigation';
import { Employee } from '@/types';
import Link from 'next/link';
import { ArrowLeft, PencilSimple, Trash } from '@phosphor-icons/react';
import styles from './page.module.scss';

export default function EmployeeDetailsPage() {
    const { id } = useParams();
    const [employee, setEmployee] = useState<Employee | null>(null);
    const [loading, setLoading] = useState(true);
    const router = useRouter();

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

    const handleDelete = async () => {
        if (!confirm('Are you sure you want to delete this employee?')) return;
        try {
            await api.delete(`/employees/${id}`);
            router.push('/employees');
        } catch (error) {
            alert('Failed to delete employee');
        }
    };

    if (loading) return <div>Loading...</div>;
    if (!employee) return <div>Employee not found</div>;

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <div className={styles.navRow}>
                    <Link href="/employees" className={styles.backLink}>
                        <ArrowLeft size={20} />
                        Back to List
                    </Link>
                    <div className={styles.actions}>
                        <Link href={`/employees/${id}/edit`} className={styles.actionBtn}>
                            <PencilSimple size={20} />
                            Edit
                        </Link>
                        <button onClick={handleDelete} className={`${styles.actionBtn} ${styles.delete}`}>
                            <Trash size={20} />
                            Delete
                        </button>
                    </div>
                </div>

                <h1 className={styles.title}>{employee.firstName} {employee.lastName}</h1>
                <span className={styles.roleBadge}>{employee.roleName}</span>
            </div>

            <div className={styles.card}>
                <div className={styles.section}>
                    <h3>Personal Information</h3>
                    <div className={styles.grid}>
                        <div className={styles.field}>
                            <label>Email</label>
                            <p>{employee.email}</p>
                        </div>
                        <div className={styles.field}>
                            <label>Document</label>
                            <p>{employee.document}</p>
                        </div>
                        <div className={styles.field}>
                            <label>Birth Date</label>
                            <p>{employee.birthDate}</p>
                        </div>
                    </div>
                </div>

                <div className={styles.section}>
                    <h3>System Information</h3>
                    <div className={styles.grid}>
                        <div className={styles.field}>
                            <label>Manager</label>
                            <p className={styles.mono}>{employee.managerName || 'N/A'}</p>
                        </div>
                        <div className={styles.field}>
                            <label>Created At</label>
                            <p>{new Date(employee.createdAtUtc).toLocaleDateString()}</p>
                        </div>
                    </div>
                </div>

                <div className={styles.section}>
                    <h3>Contact Information</h3>
                    {employee.phones && employee.phones.length > 0 ? (
                        <div className={styles.grid}>
                            {employee.phones.map((phone, index) => (
                                <div key={index} className={styles.field}>
                                    <label>{phone.type || 'Phone'} {phone.isPrimary && '(Primary)'}</label>
                                    <p>{phone.number}</p>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <p className={styles.emptyText}>No phones registered.</p>
                    )}
                </div>
            </div>
        </div>
    );
}

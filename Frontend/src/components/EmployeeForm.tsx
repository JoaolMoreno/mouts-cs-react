'use client';

import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useEffect, useState } from 'react';
import api from '@/services/api';
import { useRouter } from 'next/navigation';
import { Employee, Role } from '@/types';
import styles from './EmployeeForm.module.scss';
import { ArrowLeft } from '@phosphor-icons/react';
import Link from 'next/link';

const schema = yup.object({
    firstName: yup.string().required('First Name is required'),
    lastName: yup.string().required('Last Name is required'),
    email: yup.string().email('Invalid email').required('Email is required'),
    document: yup.string().required('Document is required'),
    roleId: yup.string().required('Role is required'),
    birthDate: yup.string().required('Birth Date is required'),
    password: yup.string().test('required-for-new', 'Password is required', function (value) {
        if (!this.options.context?.isEdit) return !!value;
        return true;
    }),
}).required();

interface EmployeeFormProps {
    initialData?: Employee;
    isEdit?: boolean;
}

export function EmployeeForm({ initialData, isEdit = false }: EmployeeFormProps) {
    const [roles, setRoles] = useState<Role[]>([]);
    const router = useRouter();
    const [error, setError] = useState('');

    const { register, handleSubmit, formState: { errors, isSubmitting }, reset } = useForm({
        resolver: yupResolver(schema),
        context: { isEdit },
        defaultValues: {
            firstName: '',
            lastName: '',
            email: '',
            document: '',
            roleId: '',
            birthDate: '',
            password: '',
        }
    });

    useEffect(() => {
        const fetchRoles = async () => {
            try {
                const response = await api.get('/roles');
                setRoles(response.data);
            } catch (error) {
                console.error('Failed to fetch roles', error);
            }
        };
        fetchRoles();

        if (initialData) {
            reset({
                ...initialData,
                roleId: initialData.roleId,
                birthDate: initialData.birthDate.split('T')[0],
                password: '',
            });
        }
    }, [initialData, reset]);

    const onSubmit = async (data: any) => {
        setError('');
        try {
            if (isEdit && initialData) {
                const { password, ...updateData } = data;
                const payload = password ? updateData : { ...updateData, password: null };
                if (!payload.password) delete payload.password;

                await api.patch(`/employees/${initialData.id}`, payload);
            } else {
                await api.post('/employees', data);
            }
            router.push('/employees');
            router.refresh();
        } catch (err: any) {
            console.error(err);
            setError(err.response?.data?.message || 'Failed to save employee.');
        }
    };

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <Link href="/employees" className={styles.backLink}>
                    <ArrowLeft size={20} />
                    Back to List
                </Link>
                <h1 className={styles.title}>{isEdit ? 'Edit Employee' : 'New Employee'}</h1>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className={styles.formCard}>
                {error && <div className={styles.error}>{error}</div>}

                <div className={styles.grid}>
                    <div className={styles.inputGroup}>
                        <label>First Name</label>
                        <input {...register('firstName')} placeholder="John" />
                        {errors.firstName && <span className={styles.errorMsg}>{errors.firstName.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Last Name</label>
                        <input {...register('lastName')} placeholder="Doe" />
                        {errors.lastName && <span className={styles.errorMsg}>{errors.lastName.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Email</label>
                        <input type="email" {...register('email')} placeholder="john@example.com" />
                        {errors.email && <span className={styles.errorMsg}>{errors.email.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Document</label>
                        <input {...register('document')} placeholder="12345678900" />
                        {errors.document && <span className={styles.errorMsg}>{errors.document.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Birth Date</label>
                        <input type="date" {...register('birthDate')} />
                        {errors.birthDate && <span className={styles.errorMsg}>{errors.birthDate.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Role</label>
                        <select {...register('roleId')}>
                            <option value="">Select Role</option>
                            {roles.map(r => (
                                <option key={r.id} value={r.id}>{r.name}</option>
                            ))}
                        </select>
                        {errors.roleId && <span className={styles.errorMsg}>{errors.roleId.message}</span>}
                    </div>

                    <div className={styles.inputGroup}>
                        <label>Password {isEdit && '(Leave blank to keep current)'}</label>
                        <input type="password" {...register('password')} placeholder="••••••" />
                        {errors.password && <span className={styles.errorMsg}>{errors.password.message}</span>}
                    </div>
                </div>

                <div className={styles.actions}>
                    <button type="submit" disabled={isSubmitting} className={styles.submitBtn}>
                        {isSubmitting ? 'Saving...' : 'Save Employee'}
                    </button>
                </div>
            </form>
        </div>
    );
}

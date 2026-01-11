'use client';

import { useEffect, useState, useRef, useCallback } from 'react';
import api from '@/services/api';
import { Employee, PagedResult } from '@/types';
import Link from 'next/link';
import { Plus, PencilSimple, Trash, Eye, CaretUp, CaretDown, Funnel } from '@phosphor-icons/react';
import styles from './page.module.scss';

type SortDirection = 'asc' | 'desc';

interface Filters {
    firstName: string;
    lastName: string;
    email: string;
    document: string;
    roleName: string;
}

const SORTABLE_COLUMNS = {
    name: 'firstName',
    email: 'email',
    role: 'roleName',
} as const;

export default function EmployeesPage() {
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const [page, setPage] = useState(1);
    const [pageSize] = useState(20);
    const [totalCount, setTotalCount] = useState(0);

    const [orderBy, setOrderBy] = useState<string | null>(null);
    const [orderDirection, setOrderDirection] = useState<SortDirection>('asc');

    const [showFilters, setShowFilters] = useState(false);
    const [showProgress, setShowProgress] = useState(false);
    const [filterInputs, setFilterInputs] = useState<Filters>({
        firstName: '',
        lastName: '',
        email: '',
        document: '',
        roleName: '',
    });
    const [appliedFilters, setAppliedFilters] = useState<Filters>({
        firstName: '',
        lastName: '',
        email: '',
        document: '',
        roleName: '',
    });

    const debounceRef = useRef<NodeJS.Timeout | null>(null);
    const animationRef = useRef<NodeJS.Timeout | null>(null);

    const fetchEmployees = useCallback(async () => {
        setIsLoading(true);
        try {
            const params = new URLSearchParams();
            params.append('page', page.toString());
            params.append('pageSize', pageSize.toString());

            if (orderBy) {
                params.append('orderBy', orderBy);
                params.append('orderDirection', orderDirection);
            }

            Object.entries(appliedFilters).forEach(([key, value]) => {
                if (value.trim()) params.append(key, value.trim());
            });

            const response = await api.get<PagedResult<Employee>>(`/employees?${params}`);
            setEmployees(response.data.items);
            setTotalCount(response.data.totalCount);
        } catch (error) {
            console.error('Failed to fetch employees', error);
        } finally {
            setIsLoading(false);
        }
    }, [page, pageSize, orderBy, orderDirection, appliedFilters]);

    useEffect(() => {
        fetchEmployees();
    }, [fetchEmployees]);

    const handleSort = (column: keyof typeof SORTABLE_COLUMNS) => {
        const field = SORTABLE_COLUMNS[column];
        if (orderBy === field) {
            setOrderDirection(prev => (prev === 'asc' ? 'desc' : 'asc'));
        } else {
            setOrderBy(field);
            setOrderDirection('asc');
        }
        setPage(1);
    };

    const handleFilterChange = (field: keyof Filters, value: string) => {
        setFilterInputs(prev => ({ ...prev, [field]: value }));

        if (debounceRef.current) clearTimeout(debounceRef.current);
        if (animationRef.current) clearTimeout(animationRef.current);
        setShowProgress(false);

        animationRef.current = setTimeout(() => {
            setShowProgress(true);
        }, 1000);

        debounceRef.current = setTimeout(() => {
            setShowProgress(false);
            setAppliedFilters(prev => ({ ...prev, [field]: value }));
            setPage(1);
        }, 3000);
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this employee?')) return;
        try {
            await api.delete(`/employees/${id}`);
            fetchEmployees();
        } catch (error) {
            alert('Failed to delete employee');
        }
    };

    const renderSortIcon = (column: keyof typeof SORTABLE_COLUMNS) => {
        const field = SORTABLE_COLUMNS[column];
        if (orderBy !== field) return null;
        return orderDirection === 'asc' ? <CaretUp size={14} weight="bold" /> : <CaretDown size={14} weight="bold" />;
    };

    const totalPages = Math.ceil(totalCount / pageSize);
    const isFirstPage = page === 1;
    const isLastPage = page >= totalPages;
    const showPagination = totalCount >= 20;

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <h1 className={styles.title}>Employees</h1>
                <div className={styles.headerActions}>
                    <button
                        onClick={() => setShowFilters(prev => !prev)}
                        className={`${styles.filterButton} ${showFilters ? styles.active : ''}`}
                    >
                        <Funnel size={20} weight={showFilters ? 'fill' : 'regular'} />
                        Filter
                    </button>
                    <Link href="/employees/new" className={styles.createButton}>
                        <Plus size={20} weight="bold" />
                        Add Employee
                    </Link>
                </div>
            </div>

            <div className={styles.tableContainer}>
                {showProgress && <div className={styles.progressBar} />}
                <table className={styles.table}>
                    <thead>
                        <tr>
                            <th onClick={() => handleSort('name')} className={styles.sortable}>
                                <span>Name {renderSortIcon('name')}</span>
                            </th>
                            <th onClick={() => handleSort('email')} className={styles.sortable}>
                                <span>Email {renderSortIcon('email')}</span>
                            </th>
                            <th onClick={() => handleSort('role')} className={styles.sortable}>
                                <span>Role {renderSortIcon('role')}</span>
                            </th>
                            <th>Actions</th>
                        </tr>
                        {showFilters && (
                            <tr className={styles.filterRow}>
                                <td>
                                    <input
                                        type="text"
                                        placeholder="Filter by name..."
                                        value={filterInputs.firstName}
                                        onChange={e => handleFilterChange('firstName', e.target.value)}
                                        className={styles.filterInput}
                                    />
                                </td>
                                <td>
                                    <input
                                        type="text"
                                        placeholder="Filter by email..."
                                        value={filterInputs.email}
                                        onChange={e => handleFilterChange('email', e.target.value)}
                                        className={styles.filterInput}
                                    />
                                </td>
                                <td>
                                    <input
                                        type="text"
                                        placeholder="Filter by role..."
                                        value={filterInputs.roleName}
                                        onChange={e => handleFilterChange('roleName', e.target.value)}
                                        className={styles.filterInput}
                                    />
                                </td>
                                <td></td>
                            </tr>
                        )}
                    </thead>
                    <tbody>
                        {isLoading ? (
                            <tr>
                                <td colSpan={4} className={styles.loading}>Loading employees...</td>
                            </tr>
                        ) : employees.length === 0 ? (
                            <tr>
                                <td colSpan={4} className={styles.empty}>No employees found.</td>
                            </tr>
                        ) : (
                            employees.map((employee) => (
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
                                            <button onClick={() => handleDelete(employee.id)} className={`${styles.actionButton} ${styles.delete}`} title="Delete" data-testid="delete-btn">
                                                <Trash size={20} />
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            {showPagination && (
                <div className={styles.pagination}>
                    <button
                        disabled={isFirstPage}
                        onClick={() => setPage(p => p - 1)}
                        className={styles.pageButton}
                    >
                        Previous
                    </button>
                    <span className={styles.pageInfo}>
                        Page {page} of {totalPages}
                    </span>
                    <button
                        disabled={isLastPage}
                        onClick={() => setPage(p => p + 1)}
                        className={styles.pageButton}
                    >
                        Next
                    </button>
                </div>
            )}
        </div>
    );
}

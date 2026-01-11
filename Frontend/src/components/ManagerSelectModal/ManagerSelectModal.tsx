'use client';

import { useState, useEffect } from 'react';
import api from '@/services/api';
import { Employee, PagedResult } from '@/types';
import { X } from '@phosphor-icons/react';
import styles from './ManagerSelectModal.module.scss';

interface ManagerSelectModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSelect: (manager: Employee | null) => void;
    currentEmployeeRank: number;
    currentEmployeeId: string;
    currentManagerId?: string;
}

export function ManagerSelectModal({
    isOpen,
    onClose,
    onSelect,
    currentEmployeeRank,
    currentEmployeeId,
    currentManagerId,
}: ManagerSelectModalProps) {
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [nameFilter, setNameFilter] = useState('');
    const [roleFilter, setRoleFilter] = useState('');

    useEffect(() => {
        if (!isOpen) return;

        const fetchEmployees = async () => {
            setIsLoading(true);
            try {
                const response = await api.get<PagedResult<Employee>>('/employees?pageSize=100');
                // Filter to only show employees with higher rank (lower rank number)
                const managers = response.data.items.filter(
                    emp => emp.rank < currentEmployeeRank && emp.id !== currentEmployeeId
                );
                setEmployees(managers);
            } catch (error) {
                console.error('Failed to fetch employees', error);
            } finally {
                setIsLoading(false);
            }
        };
        fetchEmployees();
    }, [isOpen, currentEmployeeRank, currentEmployeeId]);

    const filteredEmployees = employees.filter(emp => {
        const fullName = `${emp.firstName} ${emp.lastName}`.toLowerCase();
        const matchesName = nameFilter === '' || fullName.includes(nameFilter.toLowerCase());
        const matchesRole = roleFilter === '' || emp.roleName.toLowerCase().includes(roleFilter.toLowerCase());
        return matchesName && matchesRole;
    });

    const handleSelect = (employee: Employee) => {
        onSelect(employee);
        onClose();
    };

    const handleRemoveManager = () => {
        onSelect(null);
        onClose();
    };

    if (!isOpen) return null;

    return (
        <div className={styles.overlay} onClick={onClose}>
            <div className={styles.modal} onClick={e => e.stopPropagation()}>
                <div className={styles.header}>
                    <h2>Select Manager</h2>
                    <button onClick={onClose} className={styles.closeBtn}>
                        <X size={24} />
                    </button>
                </div>

                <div className={styles.filters}>
                    <input
                        type="text"
                        placeholder="Filter by name..."
                        value={nameFilter}
                        onChange={e => setNameFilter(e.target.value)}
                        className={styles.filterInput}
                    />
                    <input
                        type="text"
                        placeholder="Filter by role..."
                        value={roleFilter}
                        onChange={e => setRoleFilter(e.target.value)}
                        className={styles.filterInput}
                    />
                </div>

                <div className={styles.list}>
                    {currentManagerId && (
                        <button
                            onClick={handleRemoveManager}
                            className={`${styles.employeeItem} ${styles.removeOption}`}
                        >
                            <span className={styles.name}>Remove Manager</span>
                            <span className={styles.role}>Set to No Manager</span>
                        </button>
                    )}

                    {isLoading ? (
                        <div className={styles.loading}>Loading...</div>
                    ) : filteredEmployees.length === 0 ? (
                        <div className={styles.empty}>No employees found</div>
                    ) : (
                        filteredEmployees.map(emp => (
                            <button
                                key={emp.id}
                                onClick={() => handleSelect(emp)}
                                className={`${styles.employeeItem} ${emp.id === currentManagerId ? styles.selected : ''}`}
                            >
                                <span className={styles.name}>{emp.firstName} {emp.lastName}</span>
                                <span className={styles.role}>{emp.roleName}</span>
                            </button>
                        ))
                    )}
                </div>
            </div>
        </div>
    );
}

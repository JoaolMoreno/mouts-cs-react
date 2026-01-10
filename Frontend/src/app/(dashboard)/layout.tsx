'use client';

import { Navbar } from '@/components/Navbar';
import { Sidebar } from '@/components/Sidebar';
import styles from './layout.module.scss';
import { useAuth } from '@/contexts/AuthContext';
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function DashboardLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    const { isAuthenticated, isLoading } = useAuth();
    const router = useRouter();

    useEffect(() => {
        if (!isLoading && !isAuthenticated) {
            router.push('/login');
        }
    }, [isLoading, isAuthenticated, router]);

    if (isLoading) {
        return <div className={styles.loading}>Loading...</div>;
    }

    if (!isAuthenticated) {
        return null;
    }

    return (
        <div className={styles.container}>
            <Navbar />
            <div className={styles.mainWrapper}>
                <Sidebar />
                <main className={styles.content}>
                    {children}
                </main>
            </div>
        </div>
    );
}

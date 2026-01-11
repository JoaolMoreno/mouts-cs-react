'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Users } from '@phosphor-icons/react';
import styles from './Sidebar.module.scss';
import clsx from 'clsx';

const MENU_ITEMS = [
    { name: 'Employees', path: '/employees', icon: Users },
];

export function Sidebar() {
    const pathname = usePathname();

    return (
        <aside className={styles.sidebar}>
            <nav className={styles.nav}>
                {MENU_ITEMS.map((item) => {
                    const isActive = pathname.startsWith(item.path);
                    return (
                        <Link
                            key={item.path}
                            href={item.path}
                            className={clsx(styles.navItem, isActive && styles.active)}
                        >
                            <item.icon size={20} weight={isActive ? 'fill' : 'regular'} />
                            <span>{item.name}</span>
                        </Link>
                    );
                })}
            </nav>
        </aside>
    );
}

'use client';

import { useAuth } from '@/contexts/AuthContext';
import { SignOut, User as UserIcon } from '@phosphor-icons/react';
import styles from './Navbar.module.scss';
import { useState } from 'react';

export function Navbar() {
    const { user, logout } = useAuth();
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    return (
        <header className={styles.navbar}>
            <div className={styles.logo}>
                Fictional Company
            </div>

            <div className={styles.userSection}>
                <span className={styles.userName}>{user?.name}</span>

                <div className={styles.avatarContainer} onClick={() => setIsMenuOpen(!isMenuOpen)}>
                    <div className={styles.avatar}>
                        <UserIcon size={24} color="#051F1A" weight="bold" />
                    </div>

                    {isMenuOpen && (
                        <div className={styles.dropdown}>
                            <button onClick={logout} className={styles.logoutButton}>
                                <SignOut size={18} />
                                Logout
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </header>
    );
}

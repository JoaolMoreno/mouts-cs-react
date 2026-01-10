'use client';

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import api from '@/services/api';
import Cookies from 'js-cookie';

interface User {
    id: string;
    name: string; // "FirstName LastName"
    email: string;
    role: string;
    rank: number;
}

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType>({} as AuthContextType);

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const router = useRouter();

    useEffect(() => {
        const checkSession = async () => {
            try {
                await fetchUserProfile();
            } catch {
                setUser(null);
            } finally {
                setIsLoading(false);
            }
        };

        checkSession();
    }, []);

    const fetchUserProfile = async () => {
        const response = await api.get('/auth/me');
        setUser({
            id: response.data.id,
            name: `${response.data.firstName} ${response.data.lastName}`,
            email: response.data.email,
            role: response.data.roleName,
            rank: response.data.rank
        });
    };

    const login = async (email: string, password: string) => {
        try {
            await api.post('/auth/login', { email, password });
            await fetchUserProfile();
            router.push('/employees');
        } catch (error) {
            console.error('Login failed', error);
            throw error;
        }
    };

    const logout = async () => {
        try {
            await api.post('/auth/logout');
        } catch (error) {
            console.error('Logout failed', error);
        }
        setUser(null);
        router.push('/login');
    };

    return (
        <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export const useAuth = () => useContext(AuthContext);

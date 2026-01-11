import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import { AuthProvider, useAuth } from './AuthContext';


const mockGet = jest.fn();
const mockPost = jest.fn();

jest.mock('@/services/api', () => ({
    __esModule: true,
    default: {
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
    },
}));


const mockPush = jest.fn();

jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: mockPush,
    }),
}));


function TestConsumer() {
    const { user, isAuthenticated, isLoading, login, logout } = useAuth();
    return (
        <div>
            <span data-testid="is-loading">{isLoading.toString()}</span>
            <span data-testid="is-authenticated">{isAuthenticated.toString()}</span>
            <span data-testid="user-name">{user?.name || 'no-user'}</span>
            <span data-testid="user-email">{user?.email || 'no-email'}</span>
            <button onClick={() => login('test@example.com', 'password')}>Login</button>
            <button onClick={logout}>Logout</button>
        </div>
    );
}

describe('AuthContext', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    describe('Initial State', () => {
        it('starts with loading state', async () => {
            mockGet.mockImplementation(() => new Promise(() => { }));

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            expect(screen.getByTestId('is-loading').textContent).toBe('true');
        });

        it('starts with no authenticated user', async () => {
            mockGet.mockRejectedValue(new Error('Not authenticated'));

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-loading').textContent).toBe('false');
            });

            expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
            expect(screen.getByTestId('user-name').textContent).toBe('no-user');
        });
    });

    describe('Session Check', () => {
        it('fetches user profile on mount', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(mockGet).toHaveBeenCalledWith('/auth/me');
            });

            await waitFor(() => {
                expect(screen.getByTestId('user-name').textContent).toBe('John Doe');
                expect(screen.getByTestId('user-email').textContent).toBe('john@example.com');
            });
        });

        it('sets authenticated to true when user profile exists', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
            });
        });

        it('sets loading to false after session check', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-loading').textContent).toBe('false');
            });
        });
    });

    describe('Login', () => {
        it('calls login API with credentials', async () => {
            mockGet.mockRejectedValueOnce(new Error('Not authenticated'));
            mockPost.mockResolvedValue({ data: {} });
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'Test',
                    lastName: 'User',
                    email: 'test@example.com',
                    roleName: 'User',
                    rank: 2,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-loading').textContent).toBe('false');
            });

            const loginButton = screen.getByText('Login');
            await act(async () => {
                loginButton.click();
            });

            await waitFor(() => {
                expect(mockPost).toHaveBeenCalledWith('/auth/login', {
                    email: 'test@example.com',
                    password: 'password',
                });
            });
        });

        it('redirects to employees page after successful login', async () => {
            mockGet.mockRejectedValueOnce(new Error('Not authenticated'));
            mockPost.mockResolvedValue({ data: {} });
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'Test',
                    lastName: 'User',
                    email: 'test@example.com',
                    roleName: 'User',
                    rank: 2,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-loading').textContent).toBe('false');
            });

            const loginButton = screen.getByText('Login');
            await act(async () => {
                loginButton.click();
            });

            await waitFor(() => {
                expect(mockPush).toHaveBeenCalledWith('/employees');
            });
        });

        it('updates user state after successful login', async () => {
            mockGet.mockRejectedValueOnce(new Error('Not authenticated'));
            mockPost.mockResolvedValue({ data: {} });
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'Test',
                    lastName: 'User',
                    email: 'test@example.com',
                    roleName: 'User',
                    rank: 2,
                },
            });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-loading').textContent).toBe('false');
            });

            const loginButton = screen.getByText('Login');
            await act(async () => {
                loginButton.click();
            });

            await waitFor(() => {
                expect(screen.getByTestId('user-name').textContent).toBe('Test User');
            });
        });
    });

    describe('Logout', () => {
        it('calls logout API', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });
            mockPost.mockResolvedValue({ data: {} });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
            });

            const logoutButton = screen.getByText('Logout');
            await act(async () => {
                logoutButton.click();
            });

            await waitFor(() => {
                expect(mockPost).toHaveBeenCalledWith('/auth/logout');
            });
        });

        it('clears user state after logout', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });
            mockPost.mockResolvedValue({ data: {} });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('user-name').textContent).toBe('John Doe');
            });

            const logoutButton = screen.getByText('Logout');
            await act(async () => {
                logoutButton.click();
            });

            await waitFor(() => {
                expect(screen.getByTestId('user-name').textContent).toBe('no-user');
                expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
            });
        });

        it('redirects to login page after logout', async () => {
            mockGet.mockResolvedValue({
                data: {
                    id: '123',
                    firstName: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roleName: 'Admin',
                    rank: 1,
                },
            });
            mockPost.mockResolvedValue({ data: {} });

            render(
                <AuthProvider>
                    <TestConsumer />
                </AuthProvider>
            );

            await waitFor(() => {
                expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
            });

            const logoutButton = screen.getByText('Logout');
            await act(async () => {
                logoutButton.click();
            });

            await waitFor(() => {
                expect(mockPush).toHaveBeenCalledWith('/login');
            });
        });
    });
});

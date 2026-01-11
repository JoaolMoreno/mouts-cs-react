import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import LoginPage from './page';


const mockPost = jest.fn();
const mockGet = jest.fn();

jest.mock('@/services/api', () => ({
    __esModule: true,
    default: {
        post: (...args: unknown[]) => mockPost(...args),
        get: (...args: unknown[]) => mockGet(...args),
    },
}));


const mockLogin = jest.fn();

jest.mock('@/contexts/AuthContext', () => ({
    useAuth: () => ({
        login: mockLogin,
        isLoading: false,
    }),
}));

describe('LoginPage', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    describe('Rendering', () => {
        it('renders the login page title', () => {
            render(<LoginPage />);
            expect(screen.getByText('Fictional Company')).toBeInTheDocument();
        });

        it('renders the subtitle', () => {
            render(<LoginPage />);
            expect(screen.getByText('Employee Management System')).toBeInTheDocument();
        });

        it('renders the email input', () => {
            render(<LoginPage />);
            expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
        });

        it('renders the password input', () => {
            render(<LoginPage />);
            expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
        });

        it('renders the sign in button', () => {
            render(<LoginPage />);
            expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
        });

        it('renders email placeholder', () => {
            render(<LoginPage />);
            expect(screen.getByPlaceholderText('admin@example.com')).toBeInTheDocument();
        });

        it('renders password placeholder', () => {
            render(<LoginPage />);
            expect(screen.getByPlaceholderText('••••••••')).toBeInTheDocument();
        });

        it('renders footer text', () => {
            render(<LoginPage />);
            expect(screen.getByText('Access for authorized personnel only.')).toBeInTheDocument();
        });
    });

    describe('Form Interactions', () => {
        it('allows typing in email input', async () => {
            const user = userEvent.setup();
            render(<LoginPage />);

            const emailInput = screen.getByLabelText(/email/i);
            await user.type(emailInput, 'test@example.com');

            expect(emailInput).toHaveValue('test@example.com');
        });

        it('allows typing in password input', async () => {
            const user = userEvent.setup();
            render(<LoginPage />);

            const passwordInput = screen.getByLabelText(/password/i);
            await user.type(passwordInput, 'mypassword');

            expect(passwordInput).toHaveValue('mypassword');
        });

        it('toggles password visibility when clicking eye icon', async () => {
            const user = userEvent.setup();
            render(<LoginPage />);

            const passwordInput = screen.getByLabelText(/password/i);
            expect(passwordInput).toHaveAttribute('type', 'password');

            const toggleButton = document.querySelector('[class*="togglePassword"]');
            await user.click(toggleButton!);

            expect(passwordInput).toHaveAttribute('type', 'text');

            await user.click(toggleButton!);
            expect(passwordInput).toHaveAttribute('type', 'password');
        });
    });

    describe('Form Submission', () => {
        it('calls login function with email and password', async () => {
            mockLogin.mockResolvedValue(undefined);
            const user = userEvent.setup();
            render(<LoginPage />);

            await user.type(screen.getByLabelText(/email/i), 'test@example.com');
            await user.type(screen.getByLabelText(/password/i), 'password123');
            await user.click(screen.getByRole('button', { name: /sign in/i }));

            await waitFor(() => {
                expect(mockLogin).toHaveBeenCalledWith('test@example.com', 'password123');
            });
        });

        it('shows loading state during submission', async () => {
            mockLogin.mockImplementation(() => new Promise(() => { }));
            const user = userEvent.setup();
            render(<LoginPage />);

            await user.type(screen.getByLabelText(/email/i), 'test@example.com');
            await user.type(screen.getByLabelText(/password/i), 'password123');
            await user.click(screen.getByRole('button', { name: /sign in/i }));

            await waitFor(() => {
                expect(screen.getByText('Signing in...')).toBeInTheDocument();
            });
        });

        it('displays error message on login failure', async () => {
            mockLogin.mockRejectedValue(new Error('Invalid credentials'));
            const user = userEvent.setup();
            render(<LoginPage />);

            await user.type(screen.getByLabelText(/email/i), 'test@example.com');
            await user.type(screen.getByLabelText(/password/i), 'wrongpassword');
            await user.click(screen.getByRole('button', { name: /sign in/i }));

            await waitFor(() => {
                expect(screen.getByText('Invalid credentials. Please try again.')).toBeInTheDocument();
            });
        });

        it('disables submit button during submission', async () => {
            mockLogin.mockImplementation(() => new Promise(() => { }));
            const user = userEvent.setup();
            render(<LoginPage />);

            await user.type(screen.getByLabelText(/email/i), 'test@example.com');
            await user.type(screen.getByLabelText(/password/i), 'password123');

            const submitButton = screen.getByRole('button', { name: /sign in/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled();
            });
        });
    });

    describe('Accessibility', () => {
        it('has accessible labels for inputs', () => {
            render(<LoginPage />);

            expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
            expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
        });

        it('marks email and password as required', () => {
            render(<LoginPage />);

            expect(screen.getByLabelText(/email/i)).toBeRequired();
            expect(screen.getByLabelText(/password/i)).toBeRequired();
        });

        it('email input has correct type', () => {
            render(<LoginPage />);

            expect(screen.getByLabelText(/email/i)).toHaveAttribute('type', 'email');
        });
    });
});

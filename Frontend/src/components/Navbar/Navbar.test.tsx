import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Navbar } from './Navbar';

jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: jest.fn(),
    }),
}));

const mockLogout = jest.fn();
const mockUseAuth = jest.fn();

jest.mock('@/contexts/AuthContext', () => ({
    useAuth: () => mockUseAuth(),
}));

describe('Navbar', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockUseAuth.mockReturnValue({
            user: { id: '1', name: 'John Doe', email: 'john@example.com', role: 'Admin', rank: 1 },
            logout: mockLogout,
        });
    });

    describe('Rendering', () => {
        it('renders the company logo', () => {
            render(<Navbar />);
            expect(screen.getByText('Fictional Company')).toBeInTheDocument();
        });

        it('displays the current user name', () => {
            render(<Navbar />);
            expect(screen.getByText('John Doe')).toBeInTheDocument();
        });

        it('renders the avatar icon', () => {
            render(<Navbar />);
            const avatarContainer = screen.getByText('John Doe').parentElement;
            expect(avatarContainer).toBeInTheDocument();
        });

        it('does not show dropdown menu initially', () => {
            render(<Navbar />);
            expect(screen.queryByText('Logout')).not.toBeInTheDocument();
        });
    });

    describe('User Interactions', () => {
        it('toggles dropdown menu on avatar click', async () => {
            const user = userEvent.setup();
            render(<Navbar />);

            const avatarContainer = document.querySelector('[class*="avatarContainer"]');
            expect(avatarContainer).toBeInTheDocument();

            await user.click(avatarContainer!);

            expect(screen.getByText('Logout')).toBeInTheDocument();
        });

        it('hides dropdown menu when clicking avatar again', async () => {
            const user = userEvent.setup();
            render(<Navbar />);

            const avatarContainer = document.querySelector('[class*="avatarContainer"]');

            await user.click(avatarContainer!);
            expect(screen.getByText('Logout')).toBeInTheDocument();

            await user.click(avatarContainer!);
            expect(screen.queryByText('Logout')).not.toBeInTheDocument();
        });

        it('calls logout function when logout button is clicked', async () => {
            const user = userEvent.setup();
            render(<Navbar />);

            const avatarContainer = document.querySelector('[class*="avatarContainer"]');
            await user.click(avatarContainer!);

            const logoutButton = screen.getByText('Logout');
            await user.click(logoutButton);

            expect(mockLogout).toHaveBeenCalledTimes(1);
        });
    });

    describe('Edge Cases', () => {
        it('handles null user gracefully', () => {
            mockUseAuth.mockReturnValue({
                user: null,
                logout: mockLogout,
            });

            render(<Navbar />);

            expect(screen.getByText('Fictional Company')).toBeInTheDocument();
        });

        it('handles user without name', () => {
            mockUseAuth.mockReturnValue({
                user: { id: '1', email: 'john@example.com', role: 'Admin', rank: 1 },
                logout: mockLogout,
            });

            render(<Navbar />);

            expect(screen.getByText('Fictional Company')).toBeInTheDocument();
        });
    });
});

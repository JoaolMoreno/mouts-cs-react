import React from 'react';
import { render, screen } from '@testing-library/react';
import { Sidebar } from './Sidebar';


const mockUsePathname = jest.fn();

jest.mock('next/navigation', () => ({
    usePathname: () => mockUsePathname(),
}));

describe('Sidebar', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockUsePathname.mockReturnValue('/employees');
    });

    describe('Rendering', () => {
        it('renders the sidebar navigation', () => {
            render(<Sidebar />);
            expect(screen.getByRole('navigation')).toBeInTheDocument();
        });

        it('renders the Employees menu item', () => {
            render(<Sidebar />);
            expect(screen.getByText('Employees')).toBeInTheDocument();
        });

        it('renders the Employees link with correct href', () => {
            render(<Sidebar />);
            const employeesLink = screen.getByText('Employees').closest('a');
            expect(employeesLink).toHaveAttribute('href', '/employees');
        });
    });

    describe('Active State', () => {
        it('applies active class when on employees page', () => {
            mockUsePathname.mockReturnValue('/employees');
            render(<Sidebar />);

            const employeesLink = screen.getByText('Employees').closest('a');
            expect(employeesLink?.className).toContain('active');
        });

        it('applies active class when on employees subpage', () => {
            mockUsePathname.mockReturnValue('/employees/123');
            render(<Sidebar />);

            const employeesLink = screen.getByText('Employees').closest('a');
            expect(employeesLink?.className).toContain('active');
        });

        it('applies active class when on employees/new page', () => {
            mockUsePathname.mockReturnValue('/employees/new');
            render(<Sidebar />);

            const employeesLink = screen.getByText('Employees').closest('a');
            expect(employeesLink?.className).toContain('active');
        });

        it('does not apply active class when on different page', () => {
            mockUsePathname.mockReturnValue('/dashboard');
            render(<Sidebar />);

            const employeesLink = screen.getByText('Employees').closest('a');
            expect(employeesLink?.className).not.toContain('active');
        });
    });

    describe('Menu Structure', () => {
        it('contains aside element with sidebar class', () => {
            render(<Sidebar />);
            const aside = document.querySelector('aside');
            expect(aside).toBeInTheDocument();
            expect(aside?.className).toContain('sidebar');
        });

        it('contains nav element inside aside', () => {
            render(<Sidebar />);
            const nav = screen.getByRole('navigation');
            expect(nav.closest('aside')).toBeInTheDocument();
        });
    });
});

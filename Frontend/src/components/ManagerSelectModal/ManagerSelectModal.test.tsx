import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ManagerSelectModal } from './ManagerSelectModal';
import { Employee } from '@/types';

const mockGet = jest.fn();

jest.mock('@/services/api', () => ({
    __esModule: true,
    default: {
        get: (...args: unknown[]) => mockGet(...args),
    },
}));

const mockOnClose = jest.fn();
const mockOnSelect = jest.fn();

const currentEmployeeId = 'emp-1';
const currentEmployeeRank = 3;

const mockManagers: Employee[] = [
    {
        id: 'mgr-1',
        firstName: 'Alice',
        lastName: 'Manager',
        email: 'alice@example.com',
        document: 'doc-1',
        roleId: 'role-1',
        roleName: 'Boss',
        rank: 1,
        managerName: '',
        birthDate: '1980-01-01',
        createdAtUtc: '2023-01-01'
    },
    {
        id: 'mgr-2',
        firstName: 'Bob',
        lastName: 'Lead',
        email: 'bob@example.com',
        document: 'doc-2',
        roleId: 'role-2',
        roleName: 'Tech Lead',
        rank: 2,
        managerName: 'Alice Manager',
        birthDate: '1985-01-01',
        createdAtUtc: '2023-01-01'
    }
];

describe('ManagerSelectModal', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockGet.mockResolvedValue({
            data: {
                items: mockManagers,
                page: 1,
                pageSize: 100,
                totalCount: 2
            }
        });
    });

    it('renders nothing when not open', () => {
        render(
            <ManagerSelectModal
                isOpen={false}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
            />
        );

        expect(screen.queryByText('Select Manager')).not.toBeInTheDocument();
    });

    it('renders when open and fetches managers', async () => {
        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
            />
        );

        expect(screen.getByText('Select Manager')).toBeInTheDocument();
        expect(screen.getByText('Loading...')).toBeInTheDocument();

        await waitFor(() => {
            expect(mockGet).toHaveBeenCalledWith('/employees?pageSize=100');
        });

        await waitFor(() => {
            expect(screen.getByText('Alice Manager')).toBeInTheDocument();
            expect(screen.getByText('Bob Lead')).toBeInTheDocument();
        });
    });

    it('filters employees correctly by rank (logic in component calls API then filters)', async () => {
        // The component logic filters out employees with rank >= currentEmployeeRank.
        // Our mock returns valid ones. Let's add an invalid one to the mock response to test filtering.
        const invalidManager = { ...mockManagers[0], id: 'invalid', rank: 4, firstName: 'Invalid' };

        mockGet.mockResolvedValue({
            data: {
                items: [...mockManagers, invalidManager],
                page: 1,
                pageSize: 100,
                totalCount: 3
            }
        });

        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank} // 3
                currentEmployeeId={currentEmployeeId}
            />
        );

        await waitFor(() => {
            expect(screen.getByText('Alice Manager')).toBeInTheDocument(); // Rank 1 < 3
            expect(screen.getByText('Bob Lead')).toBeInTheDocument(); // Rank 2 < 3
            expect(screen.queryByText('Invalid Manager')).not.toBeInTheDocument(); // Rank 4 >= 3
        });
    });

    it('filters list by name input', async () => {
        const user = userEvent.setup();
        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
            />
        );

        await waitFor(() => {
            expect(screen.getByText('Alice Manager')).toBeInTheDocument();
        });

        const nameFilter = screen.getByPlaceholderText('Filter by name...');
        await user.type(nameFilter, 'Bob');

        expect(screen.queryByText('Alice Manager')).not.toBeInTheDocument();
        expect(screen.getByText('Bob Lead')).toBeInTheDocument();
    });

    it('filters list by role input', async () => {
        const user = userEvent.setup();
        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
            />
        );

        await waitFor(() => {
            expect(screen.getByText('Alice Manager')).toBeInTheDocument();
        });

        const roleFilter = screen.getByPlaceholderText('Filter by role...');
        await user.type(roleFilter, 'Tech Lead');

        expect(screen.queryByText('Alice Manager')).not.toBeInTheDocument(); // Role: Boss
        expect(screen.getByText('Bob Lead')).toBeInTheDocument();
    });

    it('selects a manager on click', async () => {
        const user = userEvent.setup();
        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
            />
        );

        await waitFor(() => {
            expect(screen.getByText('Alice Manager')).toBeInTheDocument();
        });

        await user.click(screen.getByText('Alice Manager'));

        expect(mockOnSelect).toHaveBeenCalledWith(expect.objectContaining({ id: 'mgr-1' }));
        expect(mockOnClose).toHaveBeenCalled();
    });

    it('shows remove manager option if currentManagerId is provided', async () => {
        const user = userEvent.setup();
        render(
            <ManagerSelectModal
                isOpen={true}
                onClose={mockOnClose}
                onSelect={mockOnSelect}
                currentEmployeeRank={currentEmployeeRank}
                currentEmployeeId={currentEmployeeId}
                currentManagerId="mgr-1"
            />
        );

        await waitFor(() => {
            expect(screen.getByText('Remove Manager')).toBeInTheDocument();
        });

        await user.click(screen.getByText('Remove Manager'));

        expect(mockOnSelect).toHaveBeenCalledWith(null);
        expect(mockOnClose).toHaveBeenCalled();
    });
});

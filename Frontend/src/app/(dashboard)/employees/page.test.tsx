import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import EmployeesPage from './page';

const mockGet = jest.fn();
const mockDelete = jest.fn();

jest.mock('@/services/api', () => ({
    __esModule: true,
    default: {
        get: (...args: unknown[]) => mockGet(...args),
        delete: (...args: unknown[]) => mockDelete(...args),
    },
}));

// Mock icons to avoid rendering issues
jest.mock('@phosphor-icons/react', () => ({
    Plus: () => <span>PlusIcon</span>,
    PencilSimple: () => <span>EditIcon</span>,
    Trash: () => <span>DeleteIcon</span>,
    Eye: () => <span>ViewIcon</span>,
    CaretUp: () => <span>UpIcon</span>,
    CaretDown: () => <span>DownIcon</span>,
    Funnel: () => <span>FilterIcon</span>,
}));

// Mock Link
jest.mock('next/link', () => ({
    __esModule: true,
    default: ({ children, href }: any) => <a href={href}>{children}</a>,
}));

// Mock SCSS
jest.mock('./page.module.scss', () => ({
    header: 'header',
    title: 'title',
    headerActions: 'headerActions',
    filterButton: 'filterButton',
    active: 'active',
    createButton: 'createButton',
    tableContainer: 'tableContainer',
    table: 'table',
    filterRow: 'filterRow',
    filterInput: 'filterInput',
    sortable: 'sortable',
    actions: 'actions',
    actionButton: 'actionButton',
    delete: 'delete',
    pagination: 'pagination',
    pageInfo: 'pageInfo',
    pageButton: 'pageButton',
    progressBar: 'progressBar',
    loading: 'loading',
}));

const mockEmployees = [
    {
        id: 'emp-1',
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        roleName: 'Developer',
        document: '123',
        rank: 2,
        managerName: 'Manager',
        createdAtUtc: '2023-01-01',
        birthDate: '1990-01-01'
    },
    {
        id: 'emp-2',
        firstName: 'Jane',
        lastName: 'Smith',
        email: 'jane@example.com',
        roleName: 'Designer',
        document: '456',
        rank: 3,
        managerName: 'Manager',
        createdAtUtc: '2023-01-02',
        birthDate: '1992-01-01'
    }
];

describe('EmployeesPage', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockGet.mockResolvedValue({
            data: {
                items: mockEmployees,
                page: 1,
                pageSize: 20,
                totalCount: 30 // Force pagination to show
            }
        });
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    it('renders and fetches employees on mount', async () => {
        render(<EmployeesPage />);

        expect(screen.getByText('Employees')).toBeInTheDocument();
        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('/employees'));

        // Wait for data
        expect(await screen.findByText('John Doe')).toBeInTheDocument();
        expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    });

    it('toggles filter inputs when filter button is clicked', async () => {
        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        await act(async () => {
            render(<EmployeesPage />);
        });

        const filterBtn = screen.getByText('Filter');
        await user.click(filterBtn);

        expect(screen.getByPlaceholderText('Filter by name...')).toBeInTheDocument();
        expect(screen.getByPlaceholderText('Filter by email...')).toBeInTheDocument();
        expect(screen.getByPlaceholderText('Filter by role...')).toBeInTheDocument();
    });

    it('handles filter debounce', async () => {
        jest.useFakeTimers();
        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        await act(async () => {
            render(<EmployeesPage />);
        });

        const filterBtn = screen.getByText('Filter');
        await user.click(filterBtn);

        const nameInput = screen.getByPlaceholderText('Filter by name...');

        fireEvent.change(nameInput, { target: { value: 'Jo' } });

        mockGet.mockClear(); // Clear initial mount call

        // Advance timers to trigger debounce (1s progress + 2s wait = 3s total)
        act(() => {
            jest.advanceTimersByTime(3000);
        });

        await waitFor(() => {
            // filters format in URL: firstName=Jo
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('firstName=Jo'));
        });
        jest.useRealTimers();
    });

    it('sorts columns when header clicked', async () => {
        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        await act(async () => {
            render(<EmployeesPage />);
        });

        await waitFor(() => {
            expect(screen.getByText('Name')).toBeInTheDocument();
        });

        mockGet.mockClear();

        const nameHeader = screen.getByText('Name');
        await user.click(nameHeader); // Sort Asc

        await waitFor(() => {
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('orderBy=firstName'));
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('orderDirection=asc'));
        });

        mockGet.mockClear();

        await user.click(nameHeader); // Sort Desc

        await waitFor(() => {
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('orderDirection=desc'));
        });
    });

    it('handles page change', async () => {
        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        await act(async () => {
            render(<EmployeesPage />);
        });

        // Wait for pagination to appear (requires totalCount > 20)
        await waitFor(() => {
            expect(screen.getByText('Next')).toBeInTheDocument();
        });

        mockGet.mockClear();

        const nextBtn = screen.getByText('Next');
        await user.click(nextBtn);

        await waitFor(() => {
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('page=2'));
        });
    });

    it('handles delete employee', async () => {
        window.confirm = jest.fn(() => true); // Mock confirm
        mockDelete.mockResolvedValue({});

        const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
        await act(async () => {
            render(<EmployeesPage />);
        });

        await waitFor(() => {
            expect(screen.getAllByRole('row').length).toBeGreaterThan(1);
        });

        const deleteBtns = screen.getAllByTestId('delete-btn');
        await user.click(deleteBtns[0]);

        expect(window.confirm).toHaveBeenCalled();
        expect(mockDelete).toHaveBeenCalledWith('/employees/emp-1');

        // Should refetch
        expect(mockGet).toHaveBeenCalledTimes(2); // Mounting + After delete
    });
});

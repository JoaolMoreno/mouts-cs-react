import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { EmployeeForm } from './EmployeeForm';

const mockGet = jest.fn();
const mockPost = jest.fn();
const mockPatch = jest.fn();

jest.mock('@/services/api', () => ({
    __esModule: true,
    default: {
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        patch: (...args: unknown[]) => mockPatch(...args),
    },
}));

const mockPush = jest.fn();
const mockRefresh = jest.fn();

jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: mockPush,
        refresh: mockRefresh,
    }),
}));

jest.mock('./ManagerSelectModal', () => ({
    ManagerSelectModal: ({ isOpen, onSelect, onClose }: any) => isOpen ? (
        <div data-testid="manager-modal">
            <button onClick={() => {
                onSelect({ id: 'mgr-999', firstName: 'New', lastName: 'Manager', roleName: 'Boss' });
                onClose();
            }}>Select New Manager</button>
            <button onClick={onClose}>Close</button>
        </div>
    ) : null
}));

const mockRoles = [
    { id: 'role-1', name: 'Manager', rank: 1 },
    { id: 'role-2', name: 'Developer', rank: 2 },
    { id: 'role-3', name: 'Designer', rank: 3 },
];

const mockEmployee = {
    id: 'emp-123',
    firstName: 'John',
    lastName: 'Doe',
    email: 'john.doe@example.com',
    document: '123456789',
    roleId: 'role-2',
    roleName: 'Developer',
    managerId: 'mgr-100',
    managerName: 'Jane Smith',
    rank: 2,
    birthDate: '1990-05-15T00:00:00Z',
    createdAtUtc: '2024-01-01T00:00:00Z',
};

const getInput = (name: string) => document.querySelector(`input[name="${name}"]`) as HTMLInputElement;
const getSelect = (name: string) => document.querySelector(`select[name="${name}"]`) as HTMLSelectElement;

describe('EmployeeForm', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockGet.mockResolvedValue({ data: mockRoles });
    });

    describe('Rendering', () => {
        it('renders the form with all required fields', async () => {
            render(<EmployeeForm />);

            expect(screen.getByText('New Employee')).toBeInTheDocument();
            expect(screen.getByText('First Name')).toBeInTheDocument();
            expect(screen.getByText('Last Name')).toBeInTheDocument();
            expect(screen.getByText('Email')).toBeInTheDocument();
            expect(screen.getByText('Document')).toBeInTheDocument();
            expect(screen.getByText('Birth Date')).toBeInTheDocument();
            expect(screen.getByText('Role')).toBeInTheDocument();
            expect(screen.getByText(/password/i)).toBeInTheDocument();
        });

        it('renders edit mode with correct title and manager section', async () => {
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            expect(screen.getByText('Edit Employee')).toBeInTheDocument();
            expect(screen.getByText('Manager')).toBeInTheDocument();
            expect(screen.getByText('Jane Smith')).toBeInTheDocument(); // Displays current manager name
        });

        it('loads and displays roles in the dropdown', async () => {
            render(<EmployeeForm />);

            await waitFor(() => {
                expect(mockGet).toHaveBeenCalledWith('/roles');
            });

            await waitFor(() => {
                expect(screen.getByText('Manager')).toBeInTheDocument();
                expect(screen.getByText('Developer')).toBeInTheDocument();
                expect(screen.getByText('Designer')).toBeInTheDocument();
            });
        });

        it('populates form with initial data in edit mode', async () => {
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            await waitFor(() => {
                expect(screen.getByDisplayValue('John')).toBeInTheDocument();
                expect(screen.getByDisplayValue('Doe')).toBeInTheDocument();
                expect(screen.getByDisplayValue('john.doe@example.com')).toBeInTheDocument();
                expect(screen.getByDisplayValue('123456789')).toBeInTheDocument();
                expect(screen.getByDisplayValue('1990-05-15')).toBeInTheDocument();
            });
        });

        it('opens manager selection modal when edit button is clicked in edit mode', async () => {
            const user = userEvent.setup();
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            const editManagerBtn = screen.getByTitle('Change Manager');
            await user.click(editManagerBtn);

            expect(screen.getByTestId('manager-modal')).toBeInTheDocument();
        });

        it('updates manager display when a manager is selected from modal', async () => {
            const user = userEvent.setup();
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            // Open modal
            await user.click(screen.getByTitle('Change Manager'));

            // Click select in mock modal
            await user.click(screen.getByText('Select New Manager'));

            expect(screen.getByText('New Manager')).toBeInTheDocument(); // firstName + lastName
        });
    });

    describe('Validation', () => {
        it('shows validation errors for empty required fields', async () => {
            const user = userEvent.setup();
            render(<EmployeeForm />);

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(screen.getByText('First Name is required')).toBeInTheDocument();
            });
        });

        it('requires password for new employees', async () => {
            const user = userEvent.setup();
            render(<EmployeeForm />);

            await waitFor(() => {
                expect(screen.getByText('Developer')).toBeInTheDocument();
            });

            await user.type(getInput('firstName'), 'Test');
            await user.type(getInput('lastName'), 'User');
            await user.type(getInput('email'), 'test@example.com');
            await user.type(getInput('document'), '123456789');

            fireEvent.change(getInput('birthDate'), { target: { value: '1990-01-01' } });

            await user.selectOptions(getSelect('roleId'), 'role-2');

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(screen.getByText('Password is required')).toBeInTheDocument();
            });
        });
    });

    describe('Form Submission', () => {
        it('submits new employee data correctly', async () => {
            mockPost.mockResolvedValue({ data: {} });
            const user = userEvent.setup();
            render(<EmployeeForm />);

            await waitFor(() => {
                expect(screen.getByText('Developer')).toBeInTheDocument();
            });

            await user.type(getInput('firstName'), 'Test');
            await user.type(getInput('lastName'), 'User');
            await user.type(getInput('email'), 'test@example.com');
            await user.type(getInput('document'), '987654321');

            fireEvent.change(getInput('birthDate'), { target: { value: '1995-06-15' } });

            await user.selectOptions(getSelect('roleId'), 'role-2');
            await user.type(getInput('password'), 'password123');

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(mockPost).toHaveBeenCalledWith('/employees', expect.objectContaining({
                    firstName: 'Test',
                    lastName: 'User',
                    email: 'test@example.com',
                    document: '987654321',
                    roleId: 'role-2',
                    password: 'password123',
                }));
            });
        });

        it('submits edit employee data correctly including changed manager', async () => {
            mockPatch.mockResolvedValue({ data: {} });
            const user = userEvent.setup();
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            await waitFor(() => {
                expect(screen.getByDisplayValue('John')).toBeInTheDocument();
            });

            // Change name
            const firstNameInput = screen.getByDisplayValue('John');
            await user.clear(firstNameInput);
            await user.type(firstNameInput, 'Johnny');

            // Change manager via modal
            await user.click(screen.getByTitle('Change Manager'));
            await user.click(screen.getByText('Select New Manager'));

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(mockPatch).toHaveBeenCalledWith(
                    `/employees/${mockEmployee.id}`,
                    expect.objectContaining({
                        firstName: 'Johnny',
                        managerId: 'mgr-999'
                    })
                );
            });
        });
    });
});

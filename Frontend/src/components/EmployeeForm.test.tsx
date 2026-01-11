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

        it('renders edit mode with correct title', async () => {
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            expect(screen.getByText('Edit Employee')).toBeInTheDocument();
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

        it('shows password field helper text in edit mode', async () => {
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            expect(screen.getByText(/leave blank to keep current/i)).toBeInTheDocument();
        });

        it('displays back to list link', () => {
            render(<EmployeeForm />);

            const backLink = screen.getByText('Back to List');
            expect(backLink).toBeInTheDocument();
            expect(backLink.closest('a')).toHaveAttribute('href', '/employees');
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

        it('prevents form submission with invalid email', async () => {
            mockPost.mockResolvedValue({ data: {} });
            const user = userEvent.setup();
            render(<EmployeeForm />);

            await waitFor(() => {
                expect(screen.getByText('Developer')).toBeInTheDocument();
            });

            await user.type(getInput('firstName'), 'Test');
            await user.type(getInput('lastName'), 'User');
            await user.type(getInput('email'), 'invalid-email');
            await user.type(getInput('document'), '123456789');
            fireEvent.change(getInput('birthDate'), { target: { value: '1990-01-01' } });
            await user.selectOptions(getSelect('roleId'), 'role-2');
            await user.type(getInput('password'), 'password123');

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await new Promise(resolve => setTimeout(resolve, 100));

            expect(mockPost).not.toHaveBeenCalled();
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

        it('does not require password for editing employees', async () => {
            mockPatch.mockResolvedValue({ data: {} });
            const user = userEvent.setup();
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            await waitFor(() => {
                expect(screen.getByDisplayValue('John')).toBeInTheDocument();
            });

            await waitFor(() => {
                expect(mockGet).toHaveBeenCalledWith('/roles');
            });

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(screen.queryByText('Password is required')).not.toBeInTheDocument();
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

        it('submits edit employee data correctly', async () => {
            mockPatch.mockResolvedValue({ data: {} });
            const user = userEvent.setup();
            render(<EmployeeForm initialData={mockEmployee} isEdit />);

            await waitFor(() => {
                expect(screen.getByDisplayValue('John')).toBeInTheDocument();
            });

            await waitFor(() => {
                expect(screen.getByText('Developer')).toBeInTheDocument();
            });

            const firstNameInput = screen.getByDisplayValue('John');
            await user.clear(firstNameInput);
            await user.type(firstNameInput, 'Johnny');

            const submitButton = screen.getByRole('button', { name: /save employee/i });
            await user.click(submitButton);

            await waitFor(() => {
                expect(mockPatch).toHaveBeenCalledWith(
                    `/employees/${mockEmployee.id}`,
                    expect.objectContaining({
                        firstName: 'Johnny',
                    })
                );
            });
        });

        it('redirects to employees list after successful submission', async () => {
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
                expect(mockPush).toHaveBeenCalledWith('/employees');
                expect(mockRefresh).toHaveBeenCalled();
            });
        });

        it('displays error message on submission failure', async () => {
            mockPost.mockRejectedValue({
                response: { data: { message: 'Email already exists' } },
            });
            const user = userEvent.setup();
            render(<EmployeeForm />);

            // Wait for roles to load
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
                expect(screen.getByText('Email already exists')).toBeInTheDocument();
            });
        });

        it('shows loading state during submission', async () => {
            mockPost.mockImplementation(() => new Promise(() => { }));
            const user = userEvent.setup();
            render(<EmployeeForm />);

            // Wait for roles to load
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
                expect(screen.getByText('Saving...')).toBeInTheDocument();
            });
        });
    });
});

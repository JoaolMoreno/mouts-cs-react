import '@testing-library/jest-dom';

// Mock Next.js router
jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: jest.fn(),
        refresh: jest.fn(),
        back: jest.fn(),
        forward: jest.fn(),
        replace: jest.fn(),
        prefetch: jest.fn(),
    }),
    usePathname: () => '/employees',
    useParams: () => ({ id: '123' }),
    useSearchParams: () => new URLSearchParams(),
}));

// Mock window.confirm and window.alert
Object.defineProperty(window, 'confirm', {
    writable: true,
    value: jest.fn().mockImplementation(() => true),
});

Object.defineProperty(window, 'alert', {
    writable: true,
    value: jest.fn(),
});

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: jest.fn().mockImplementation(query => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
    })),
});

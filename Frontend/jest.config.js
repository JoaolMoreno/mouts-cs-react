const nextJest = require('next/jest');

const createJestConfig = nextJest({
    dir: './',
});

/** @type {import('jest').Config} */
const config = {
    displayName: 'Frontend Tests',
    testEnvironment: 'jsdom',
    setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
    moduleNameMapper: {
        '^@/(.*)$': '<rootDir>/src/$1',
        '\\.(css|scss|sass)$': 'identity-obj-proxy',
    },
    testMatch: [
        '<rootDir>/src/**/*.test.{ts,tsx}',
        '<rootDir>/src/**/*.spec.{ts,tsx}',
    ],
    collectCoverageFrom: [
        'src/**/*.{ts,tsx}',
        '!src/**/*.d.ts',
        '!src/**/*.module.scss',
    ],
    transformIgnorePatterns: [
        '/node_modules/',
        '^.+\\.module\\.(css|sass|scss)$',
    ],
};

module.exports = createJestConfig(config);

export default {
    testEnvironment: 'jsdom',
    testEnvironmentOptions: {
        customExportConditions: ["node", "node-addons"],
    },
    moduleFileExtensions: ['js', 'ts', 'vue', 'json', 'mjs'],
    transform: {
        '^.+\\.ts$': 'ts-jest',
        '^.+\\.vue$': '@vue/vue3-jest',
        '^.+\\.(js|mjs)$': 'babel-jest',
    },
    moduleNameMapper: {
        '^~/(.*)$': '<rootDir>/$1',
        '^@/(.*)$': '<rootDir>/$1',
        '^#app(.*)$': '<rootDir>/.nuxt/app$1',
        '^#imports(.*)$': '<rootDir>/tests/mocks/imports.ts',
        '^lucide-vue-next$': '<rootDir>/tests/mocks/lucide-vue-next.ts',
        '^@vue/test-utils$': '<rootDir>/node_modules/@vue/test-utils/dist/vue-test-utils.cjs.js',
    },
    testPathIgnorePatterns: ['<rootDir>/.nuxt/', '<rootDir>/node_modules/'],
    setupFilesAfterEnv: ['<rootDir>/jest.setup.mjs'],
    collectCoverage: true,
    collectCoverageFrom: [
        '<rootDir>/components/**/*.vue',
        '<rootDir>/pages/**/*.vue',
        '<rootDir>/utils/**/*.ts',
        '<rootDir>/composables/**/*.ts',
    ],
};

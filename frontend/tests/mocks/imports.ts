import { jest } from "@jest/globals";

export const useAuth = () => ({ signIn: jest.fn(), signOut: jest.fn() });
export const useRouter = () => ({ push: jest.fn() });
export const definePageMeta = () => { };

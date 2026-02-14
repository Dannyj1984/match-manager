import { describe, it, expect, jest, beforeEach, beforeAll } from '@jest/globals'
import { mount, flushPromises } from '@vue/test-utils'
import Login from '../../pages/login.vue'
import { ref, reactive } from 'vue'

const mockSignIn = jest.fn()
const mockPush = jest.fn()

describe('Login.vue', () => {
    beforeAll(() => {
        (window as any).useAuth = () => ({
            signIn: mockSignIn
        });
        (window as any).useRouter = () => ({
            push: mockPush
        });
        (window as any).definePageMeta = jest.fn();
        (window as any).reactive = reactive;
        (window as any).ref = ref;
    });

    beforeEach(() => {
        jest.clearAllMocks()
    })

    it('renders the login form correctly', () => {
        const wrapper = mount(Login, {
            global: {
                stubs: {
                    ShieldCheck: true,
                    Mail: true,
                    Lock: true,
                    Loader2: true,
                    LogIn: true
                }
            }
        })

        expect(wrapper.find('input[type="email"]').exists()).toBe(true)
        expect(wrapper.find('input[type="password"]').exists()).toBe(true)
        expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
        expect(wrapper.text()).toContain('Login')
    })

    it('handles successful login', async () => {
        mockSignIn.mockResolvedValue({} as never) // Success

        const wrapper = mount(Login, {
            global: {
                stubs: {
                    ShieldCheck: true,
                    Mail: true,
                    Lock: true,
                    Loader2: true,
                    LogIn: true
                }
            }
        })

        const emailInput = wrapper.find('input[type="email"]')
        const passwordInput = wrapper.find('input[type="password"]')
        const form = wrapper.find('form')

        await emailInput.setValue('test@example.com')
        await passwordInput.setValue('password123')
        await form.trigger('submit')

        expect(mockSignIn).toHaveBeenCalledWith(
            expect.objectContaining({
                email: 'test@example.com',
                password: 'password123'
            }),
            { redirect: false }
        )

        await flushPromises()

        expect(mockPush).toHaveBeenCalledWith('/')
    })

    it('handles login failure (invalid credentials)', async () => {
        mockSignIn.mockResolvedValue({ error: 'Invalid credentials' } as never)

        const wrapper = mount(Login, {
            global: {
                stubs: {
                    ShieldCheck: true,
                    Mail: true,
                    Lock: true,
                    Loader2: true,
                    LogIn: true
                }
            }
        })

        const form = wrapper.find('form')
        await form.trigger('submit')

        await flushPromises()

        expect(wrapper.text()).toContain('Invalid email or password')
        expect(mockPush).not.toHaveBeenCalled()
    })

    it('handles unexpected errors during login', async () => {
        mockSignIn.mockRejectedValue(new Error('Network error') as never)

        const wrapper = mount(Login, {
            global: {
                stubs: {
                    ShieldCheck: true,
                    Mail: true,
                    Lock: true,
                    Loader2: true,
                    LogIn: true
                }
            }
        })

        const form = wrapper.find('form')
        await form.trigger('submit')

        await flushPromises()

        expect(wrapper.text()).toContain('An error occurred during login')
        expect(mockPush).not.toHaveBeenCalled()
    })
    it('Shows the link to register', () => {
        const wrapper = mount(Login, {
            global: {
                stubs: {
                    ShieldCheck: true,
                    Mail: true,
                    Lock: true,
                    Loader2: true,
                    LogIn: true
                }
            }
        })
        expect(wrapper.text()).toContain('Register as a player')
        expect(wrapper.find('a').attributes('href')).toBe('#')
    })
})

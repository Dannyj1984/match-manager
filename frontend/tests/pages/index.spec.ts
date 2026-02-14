import { describe, it, expect, jest, beforeEach, beforeAll } from '@jest/globals'
import { mount } from '@vue/test-utils'
import Dashboard from '../../pages/index.vue'
import { ref, reactive, computed, watch, watchEffect } from 'vue'

import { useMatchStore } from '../../stores/match'

// Mocking composables
const mockUseAuth = jest.fn()
const mockUseLeague = jest.fn()
const mockNavigateTo = jest.fn()

jest.mock('../../stores/match', () => ({
    useMatchStore: jest.fn()
}))



jest.mock('../../utils/format-helpers', () => ({
    formatDate: (date: string) => date
}))

const mockSignIn = jest.fn()
const mockPush = jest.fn()


describe('Dashboard (index.vue)', () => {
    beforeEach(() => {
        jest.clearAllMocks()
    })
    beforeAll(() => {
        (window as any).useAuth = mockUseAuth;
        (window as any).useLeague = mockUseLeague;
        (window as any).navigateTo = mockNavigateTo;
        (window as any).useRouter = () => ({
            push: mockPush
        });
        (window as any).definePageMeta = jest.fn();
        (window as any).reactive = reactive;
        (window as any).ref = ref;
        (window as any).computed = computed;
        (window as any).watch = watch;
        (window as any).watchEffect = watchEffect;
    })

    it('renders correctly when not logged in', () => {
        mockUseAuth.mockReturnValue({
            status: ref('unauthenticated'),
            data: ref(null)
        })

            ; (useMatchStore as unknown as jest.Mock).mockReturnValue({
                fetchDashboard: jest.fn()
            })
        mockUseLeague.mockReturnValue({
            currentLeague: ref(null)
        })

        const wrapper = mount(Dashboard, {
            global: {
                stubs: {
                    NuxtLink: {
                        template: '<a :href="to"><slot /></a>',
                        props: ['to']
                    }
                }
            }
        })

        expect(wrapper.text()).toContain('WELCOME BACK')

        expect(wrapper.text()).toContain('READY TO JOIN THE GAME?')
        expect(wrapper.text()).toContain('Login to access your personalised dashboard')

        const getStartedLink = wrapper.find('a[href="/login"]')
        expect(getStartedLink.exists()).toBe(true)
        expect(getStartedLink.text()).toContain('Get Started')

        expect(wrapper.text()).not.toContain('Recent Results')
        expect(wrapper.text()).not.toContain('Next Match')
        expect(wrapper.text()).not.toContain('Recent Performance')
    })
    it('Render correct login link', () => {
        mockUseAuth.mockReturnValue({
            status: ref('unauthenticated'),
            data: ref(null)
        })

            ; (useMatchStore as unknown as jest.Mock).mockReturnValue({
                fetchDashboard: jest.fn()
            })
        mockUseLeague.mockReturnValue({
            currentLeague: ref(null)
        })

        const wrapper = mount(Dashboard, {
            global: {
                stubs: {
                    NuxtLink: {
                        template: '<a :href="to"><slot /></a>',
                        props: ['to']
                    },
                    // Stubs for icons to avoid warnings/errors if full render is attempted
                    Trophy: true,
                    ChevronRight: true,
                    Activity: true,
                    Calendar: true,
                    Star: true,
                    TrendingUp: true
                }
            }
        })

        const getStartedLink = wrapper.find('a[href="/login"]')
        expect(getStartedLink.exists()).toBe(true)
        expect(getStartedLink.attributes('href')).toBe('/login')
    })
    it('Navigates to login when get started is clicked', () => {
        mockUseAuth.mockReturnValue({
            status: ref('unauthenticated'),
            data: ref(null)
        })

            ; (useMatchStore as unknown as jest.Mock).mockReturnValue({
                fetchDashboard: jest.fn()
            })
        mockUseLeague.mockReturnValue({
            currentLeague: ref(null)
        })

        const wrapper = mount(Dashboard, {
            global: {
                mocks: {
                    $router: {
                        push: mockPush
                    }
                },
                stubs: {
                    NuxtLink: {
                        template: '<a @click="$router.push(to)"><slot /></a>',
                        props: ['to']
                    }
                }
            }
        })

        const getStartedLink = wrapper.find('a')
        expect(getStartedLink.exists()).toBe(true)
        getStartedLink.trigger('click')
        expect(mockPush).toHaveBeenCalledWith('/login')

    })
})

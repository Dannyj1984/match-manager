import type { AuthSessionData } from "~/types/auth"

export default defineNuxtRouteMiddleware((to, from) => {
    const { status, data } = useAuth()

    // Guest only middleware for login/register
    if (to.meta.middleware === 'guest-only') {
        if (status.value === 'authenticated') {
            return navigateTo('/')
        }
        return
    }

    // Admin protection
    if (to.path.startsWith('/admin') || to.path.includes('/calculate-teams')) {
        const userRole = (data.value as AuthSessionData | null)?.user?.roles || []
        if (status.value !== 'authenticated' || !userRole.includes('Admin')) {
            return navigateTo('/login')
        }
    }

    // Default protected routes (optionally add logic here or use per-page)
})

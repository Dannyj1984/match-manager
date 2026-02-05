// Middleware to redirect to leagues page if no league is selected
export default defineNuxtRouteMiddleware(async (to) => {
    // Skip check for auth-related pages and leagues pages
    if (to.path.startsWith('/login') ||
        to.path.startsWith('/register') ||
        to.path.startsWith('/leagues')) {
        return
    }

    const { status } = useAuth()

    // Skip if not authenticated (allow guest access to pages using this middleware)
    if (status.value !== 'authenticated') {
        return
    }

    const currentLeague = useState<any>('currentLeague')

    // If no current league, redirect to leagues page
    if (!currentLeague.value) {
        return navigateTo('/leagues')
    }
})

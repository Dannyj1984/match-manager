export default defineNuxtRouteMiddleware((to) => {
    const { currentLeague } = useLeague()

    // If no league selected, let league middleware handle it
    if (!currentLeague.value) return

    // "SuperAdmin" role implies they are viewing via global override, not membership
    if (currentLeague.value.role === 'SuperAdmin') {
        return navigateTo('/')
    }
})

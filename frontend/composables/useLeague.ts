export const useLeague = () => {
    const currentLeague = useState<any>('currentLeague', () => null)
    const userLeagues = useState<any[]>('userLeagues', () => [])
    const { token } = useAuth()

    const fetchUserLeagues = async () => {
        try {
            const response = await $fetch('/api/leagues', {
                headers: {
                    Authorization: `${token.value}`
                }
            })
            userLeagues.value = response as any[]

            // Try to restore previously selected league from localStorage
            const savedLeagueId = process.client ? localStorage.getItem('selectedLeagueId') : null

            if (savedLeagueId) {
                const savedLeague = userLeagues.value.find(l => l.id === savedLeagueId)
                if (savedLeague) {
                    currentLeague.value = savedLeague
                    return
                }
            }

            // Set first league as current if none selected
            if (!currentLeague.value && userLeagues.value.length > 0) {
                currentLeague.value = userLeagues.value[0]
                if (process.client) {
                    localStorage.setItem('selectedLeagueId', currentLeague.value.id)
                }
            }
        } catch (error) {
            console.error('Failed to fetch leagues:', error)
        }
    }

    const setCurrentLeague = (league: any) => {
        currentLeague.value = league
        // Persist selection to localStorage
        if (process.client && league) {
            localStorage.setItem('selectedLeagueId', league.id)
        }
    }

    const clearLeague = () => {
        currentLeague.value = null
        userLeagues.value = []
        if (process.client) {
            localStorage.removeItem('selectedLeagueId')
        }
    }

    const createLeague = async (leagueData: {
        name: string
        sport: string
        maxTeams: number
        location?: string
        description?: string
        initialAdminUserId?: string
    }) => {
        try {
            const response = await $fetch('/api/leagues', {
                method: 'POST',
                headers: {
                    Authorization: `${token.value}`,
                    'Content-Type': 'application/json'
                },
                body: leagueData
            })
            await fetchUserLeagues()
            return response
        } catch (error) {
            console.error('Failed to create league:', error)
            throw error
        }
    }

    const updateLeague = async (leagueId: string, leagueData: {
        name: string
        sport: string
        maxTeams: number
        location?: string
        description?: string
        allowRatings?: boolean
    }) => {
        try {
            await $fetch(`/api/leagues/${leagueId}`, {
                method: 'PUT',
                headers: {
                    Authorization: `${token.value}`,
                    'Content-Type': 'application/json',
                    'X-League-Id': leagueId
                },
                body: leagueData
            })
            await fetchUserLeagues()
        } catch (error) {
            console.error('Failed to update league:', error)
            throw error
        }
    }

    const getLeagueMembers = async (leagueId: string) => {
        try {
            const response = await $fetch(`/api/leagues/${leagueId}/members`, {
                headers: {
                    Authorization: `${token.value}`,
                    'X-League-Id': leagueId
                }
            })
            return response
        } catch (error) {
            console.error('Failed to fetch members:', error)
            throw error
        }
    }

    const addMember = async (leagueId: string, email: string) => {
        try {
            await $fetch(`/api/leagues/${leagueId}/members`, {
                method: 'POST',
                headers: {
                    Authorization: `${token.value}`,
                    'Content-Type': 'application/json',
                    'X-League-Id': leagueId
                },
                body: { email }
            })
        } catch (error) {
            console.error('Failed to add member:', error)
            throw error
        }
    }

    const removeMember = async (leagueId: string, userId: string) => {
        try {
            await $fetch(`/api/leagues/${leagueId}/members/${userId}`, {
                method: 'DELETE',
                headers: {
                    Authorization: `${token.value}`,
                    'X-League-Id': leagueId
                }
            })
        } catch (error) {
            console.error('Failed to remove member:', error)
            throw error
        }
    }

    const promoteToAdmin = async (leagueId: string, userId: string) => {
        try {
            await $fetch(`/api/leagues/${leagueId}/admins/${userId}`, {
                method: 'POST',
                headers: {
                    Authorization: `${token.value}`,
                    'X-League-Id': leagueId
                }
            })
        } catch (error) {
            console.error('Failed to promote user:', error)
            throw error
        }
    }

    const demoteAdmin = async (leagueId: string, userId: string) => {
        try {
            await $fetch(`/api/leagues/${leagueId}/admins/${userId}`, {
                method: 'DELETE',
                headers: {
                    Authorization: `${token.value}`,
                    'X-League-Id': leagueId
                }
            })
        } catch (error) {
            console.error('Failed to demote user:', error)
            throw error
        }
    }

    const getLeague = async (leagueId: string) => {
        try {
            const response = await $fetch(`/api/leagues/${leagueId}`, {
                headers: {
                    Authorization: `${token.value}`,
                    'X-League-Id': leagueId
                }
            })
            return response
        } catch (error) {
            console.error('Failed to fetch league:', error)
            throw error
        }
    }

    const createLeagueAdmin = async (leagueId: string, adminData: {
        email: string
        fullName: string
        password: string
        preferredPosition?: string[]
    }) => {
        try {
            const response = await $fetch(`/api/leagues/${leagueId}/create-admin`, {
                method: 'POST',
                headers: {
                    Authorization: `${token.value}`,
                    'Content-Type': 'application/json',
                    'X-League-Id': leagueId
                },
                body: adminData
            })
            return response
        } catch (error) {
            console.error('Failed to create league admin:', error)
            throw error
        }
    }

    return {
        currentLeague,
        userLeagues,
        fetchUserLeagues,
        setCurrentLeague,
        clearLeague,
        createLeague,
        updateLeague,
        getLeague,
        getLeagueMembers,
        addMember,
        removeMember,
        promoteToAdmin,
        demoteAdmin,
        createLeagueAdmin
    }
}

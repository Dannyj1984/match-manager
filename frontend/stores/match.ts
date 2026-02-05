import { defineStore } from 'pinia'

export interface Player {
    id: string
    fullName: string
    currentRating: number
    preferredPosition: string
    lastPlayed?: string
}

export interface MatchAssignment {
    playerId: string
    teamNumber: number
}

export const useMatchStore = defineStore('match', {
    state: () => ({
        availablePlayers: [] as Player[],
        selectedPlayerIds: [] as string[],
        teams: {} as Record<number, Player[]>,
        matchStatus: 'Draft' as 'Draft' | 'Active' | 'Completed',
        isLoading: false,
        user: null as any,
        currentPlayerId: null as string | null,

        // New Draft Config
        teamsCount: 2,
        matchDate: new Date().toISOString().split('T')[0],
        formatType: '5v5',
        nextMatchDate: null as string | null,
        currentMatchId: null as string | null,
        isCompleted: false,
        canRate: false
    }),

    getters: {
        selectedPlayers: (state) => state.availablePlayers.filter(p => state.selectedPlayerIds.includes(p.id)),
        teamPower: (state) => (teamNumber: number) => {
            return state.teams[teamNumber]?.reduce((sum, p) => sum + Number(p.currentRating), 0) || 0
        },
        powerBalance: (state) => {
            const teamNumbers = Object.keys(state.teams).map(Number)
            if (teamNumbers.length < 2) return "0.0"

            const powers = teamNumbers.map(n =>
                state.teams[n].reduce((sum, p) => sum + Number(p.currentRating), 0)
            )

            const max = Math.max(...powers)
            const min = Math.min(...powers)
            return (max - min).toFixed(1)
        }
    },

    actions: {
        async fetchPlayers() {
            const { fetch } = useApi()
            try {
                this.availablePlayers = await fetch<Player[]>('/players')
            } catch (error) {
                console.error('Failed to fetch players', error)
            }
        },

        async createPlayer(playerData: { email: string, fullName: string, initialPassword: string, initialRating: number, preferredPosition: string, leagueId?: string }) {
            const { fetch } = useApi()
            try {
                const headers: Record<string, string> = {}
                if (playerData.leagueId) {
                    headers['X-League-Id'] = playerData.leagueId
                }

                const newPlayer = await fetch<Player>('/players', {
                    method: 'POST',
                    headers,
                    body: playerData
                })
                this.availablePlayers.push(newPlayer)
                this.availablePlayers.sort((a, b) => b.currentRating - a.currentRating)
                return { success: true }
            } catch (error: any) {
                console.error('Failed to create player', error)
                return { success: false, error: error.data?.message || 'Failed to create player' }
            }
        },

        async promoteToAdmin(playerId: string) {
            const { fetch } = useApi()
            try {
                await fetch(`/players/${playerId}/promote`, {
                    method: 'POST'
                })
                return { success: true }
            } catch (error: any) {
                console.error('Failed to promote player', error)
                return { success: false, error: error.data?.message || 'Failed to promote player' }
            }
        },

        async updatePlayerRating(playerId: string, newRating: number) {
            const { fetch } = useApi()
            try {
                const updated = await fetch<Player>(`/players/${playerId}/rating`, {
                    method: 'PATCH',
                    body: { newRating }
                })
                // Update local state
                const index = this.availablePlayers.findIndex(p => p.id === playerId)
                if (index !== -1) {
                    this.availablePlayers[index].currentRating = updated.currentRating
                    this.availablePlayers.sort((a, b) => b.currentRating - a.currentRating)
                }
                return { success: true }
            } catch (error: any) {
                console.error('Failed to update rating', error)
                return { success: false, error: error.data?.message || 'Failed to update rating' }
            }
        },

        async generateTeams() {
            if (this.selectedPlayerIds.length === 0) return

            const { fetch } = useApi()
            this.isLoading = true
            try {
                const assignments = await fetch<MatchAssignment[]>('/matches/calculate-teams', {
                    method: 'POST',
                    body: {
                        playerIds: this.selectedPlayerIds,
                        teamCount: this.teamsCount
                    }
                })

                // Reset teams
                this.teams = {}
                for (let i = 1; i <= this.teamsCount; i++) {
                    this.teams[i] = []
                }

                // Assign players based on backend results
                assignments.forEach(a => {
                    const player = this.availablePlayers.find(p => p.id === a.playerId)
                    if (player) {
                        if (!this.teams[a.teamNumber]) this.teams[a.teamNumber] = []
                        this.teams[a.teamNumber].push(player)
                    }
                })
            } catch (error) {
                console.error('Failed to generate teams', error)
            } finally {
                this.isLoading = false
            }
        },

        async saveMatch() {
            const { fetch } = useApi()
            this.isLoading = true
            try {
                const assignments = []
                for (const [teamNum, players] of Object.entries(this.teams)) {
                    for (const player of players) {
                        assignments.push({
                            playerId: player.id,
                            teamNumber: parseInt(teamNum)
                        })
                    }
                }

                await fetch('/matches', {
                    method: 'POST',
                    body: {
                        date: this.matchDate,
                        formatType: this.formatType,
                        assignments: assignments
                    }
                })
                return { success: true }
            } catch (error: any) {
                console.error('Failed to save match', error)
                return { success: false, error: error.data?.message || 'Failed to save match' }
            } finally {
                this.isLoading = false
            }
        },

        async fetchMatchByDate(date: string) {
            const { fetch } = useApi()
            this.isLoading = true

            // Clear state immediately to avoid showing stale data from previous dates
            this.teams = {}
            this.selectedPlayerIds = []
            this.nextMatchDate = null
            this.currentMatchId = null
            this.isCompleted = false
            this.canRate = false

            try {
                const match = await fetch<any>(`/matches/by-date/${date}`)
                console.log('[MatchStore] Fetched match:', match)
                if (!match) return { success: false }

                const returnedDate = (match.date || match.Date)?.split('T')[0]

                if (returnedDate === date) {
                    console.log('[MatchStore] Exact date match found:', returnedDate)
                    this.currentMatchId = match.id || match.Id
                    this.isCompleted = match.isCompleted || match.IsCompleted

                    const newTeams: Record<number, Player[]> = {}
                    const newSelectedIds: string[] = []
                    let maxTeam = 0

                    const assignments = match.matchAssignments || match.MatchAssignments || []

                    assignments.forEach((a: any) => {
                        const playerId = a.playerId || a.PlayerId
                        const teamNumber = a.teamNumber || a.TeamNumber
                        const rawPlayer = a.player || a.Player

                        if (playerId) newSelectedIds.push(playerId)
                        if (teamNumber && rawPlayer) {
                            const player: Player = {
                                id: rawPlayer.id || rawPlayer.Id,
                                fullName: rawPlayer.fullName || rawPlayer.FullName,
                                currentRating: rawPlayer.currentRating || rawPlayer.CurrentRating,
                                preferredPosition: rawPlayer.preferredPosition || rawPlayer.PreferredPosition
                            }
                            if (!newTeams[teamNumber]) newTeams[teamNumber] = []
                            newTeams[teamNumber].push(player)
                            if (teamNumber > maxTeam) maxTeam = teamNumber
                        }
                    })

                    // Fallback to current teamsCount if no assignments
                    if (maxTeam === 0) maxTeam = this.teamsCount || 2

                    for (let i = 1; i <= maxTeam; i++) {
                        if (!newTeams[i]) newTeams[i] = []
                    }

                    this.selectedPlayerIds = newSelectedIds
                    this.teams = newTeams
                    this.teamsCount = maxTeam
                    this.formatType = match.formatType || match.FormatType || '5v5'
                    console.log('[MatchStore] State updated. Teams:', Object.keys(this.teams).length, 'Players:', this.selectedPlayerIds.length)

                    // Check if user can rate this match
                    if (this.isCompleted && this.currentMatchId) {
                        await this.checkCanRate(this.currentMatchId)
                    }
                } else {
                    console.log('[MatchStore] Future match found:', returnedDate)
                    this.nextMatchDate = returnedDate
                }

                return { success: true }
            } catch (error: any) {
                if (error.status !== 404) {
                    console.error('Failed to fetch match', error)
                }
                return { success: false, error: 'No match found for this date' }
            } finally {
                this.isLoading = false
            }
        },

        movePlayer(playerId: string, targetTeam: number) {
            // Find player across all teams
            let player: Player | undefined
            for (const teamNum in this.teams) {
                const index = this.teams[Number(teamNum)].findIndex(p => p.id === playerId)
                if (index > -1) {
                    player = this.teams[Number(teamNum)].splice(index, 1)[0]
                    break
                }
            }

            if (player) {
                if (!this.teams[targetTeam]) this.teams[targetTeam] = []
                this.teams[targetTeam].push(player)
            }
        },

        async fetchMe() {
            const { fetch } = useApi()
            try {
                return await fetch<any>('/players/me')
            } catch (error) {
                console.error('Failed to fetch profile', error)
                return null
            }
        },

        async updateProfile(profileData: { fullName: string, preferredPosition: string }) {
            const { fetch } = useApi()
            this.isLoading = true
            try {
                await fetch('/players/me', {
                    method: 'PUT',
                    body: profileData
                })
                // Refresh local players if needed
                await this.fetchPlayers()
                return { success: true }
            } catch (error: any) {
                console.error('Failed to update profile', error)
                return { success: false, error: error.data?.message || 'Failed to update profile' }
            } finally {
                this.isLoading = false
            }
        },

        async fetchDashboard() {
            const { fetch } = useApi()
            try {
                return await fetch<any>('/matches/dashboard')
            } catch (error) {
                console.error('Failed to fetch dashboard', error)
                return null
            }
        },

        async completeMatch(matchId: string) {
            const { fetch } = useApi()
            this.isLoading = true
            try {
                await fetch(`/matches/${matchId}/complete`, {
                    method: 'PATCH'
                })
                return { success: true }
            } catch (error: any) {
                console.error('Failed to complete match', error)
                return { success: false, error: error.data?.message || 'Failed to complete match' }
            } finally {
                this.isLoading = false
            }
        },

        async submitRatings(matchId: string, ratings: { subjectId: string, value: number }[]) {
            const { fetch } = useApi()
            this.isLoading = true
            try {
                await fetch(`/matches/${matchId}/ratings`, {
                    method: 'POST',
                    body: ratings
                })
                return { success: true }
            } catch (error: any) {
                console.error('Failed to submit ratings', error)
                return { success: false, error: error.data?.message || 'Failed to submit ratings' }
            } finally {
                this.isLoading = false
            }
        },

        async fetchMyRatings(matchId: string) {
            const { fetch } = useApi()
            try {
                return await fetch<any[]>(`/matches/${matchId}/my-ratings`)
            } catch (error) {
                console.error('Failed to fetch my ratings', error)
                return []
            }
        },

        async checkCanRate(matchId: string) {
            const { fetch } = useApi()
            try {
                const result = await fetch<{ canRate: boolean }>(`/matches/${matchId}/can-rate`)
                this.canRate = result.canRate
            } catch (error) {
                console.error('Failed to check can rate', error)
                this.canRate = false
            }
        }
    }
})

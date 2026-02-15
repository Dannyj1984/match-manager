import type { Player } from './player'

/** Match assignment linking a player to a team */
export interface MatchAssignment {
    playerId: string
    teamNumber: number
    player?: Player
}

/** Match as returned by GET /api/matches/:id */
export interface Match {
    id: string
    leagueId: string
    date: string
    location: string | null
    formatType: string
    isCompleted: boolean
    allowRatings: boolean | null
    matchAssignments: MatchAssignment[]
}

/** Dashboard data as returned by GET /api/matches/dashboard */
export interface DashboardData {
    lastCompletedMatchDate: string | null
    nextActiveMatchDate: string | null
    pendingRatingMatchDate?: string | null
    recentPerformance: { date: string; value: number }[]
    needsLeague?: boolean
}

/** Raw rating as returned by GET /api/matches/:id/my-ratings */
export interface RawRating {
    subjectId: string
    value: number
}

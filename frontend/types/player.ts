/** Player stats as returned inline by GET /api/players */
export interface PlayerStats {
    gamesPlayed: number
    currentStreak: number
    highestRating4Weeks: number | null
    isGoldenBoot: boolean
    earlyBirdCount: number
    // aliases used by enrichment on the players page
    streak?: number
    rating4Weeks?: number | null
    earlyBird?: number
}

/** Player as returned by GET /api/players (enriched with role + stats) */
export interface Player {
    id: string
    fullName: string
    currentRating: number
    avgMatchRating?: number
    preferredPosition: string[]
    lastPlayed: string | null
    identityUserId: string | null
    role: string | null
    stats: PlayerStats
}

/** Player profile as returned by GET /api/players/me */
export interface PlayerProfile {
    id: string
    fullName: string
    currentRating: number
    preferredPosition: string[]
    email: string
}

/** Leaderboard entry as returned by GET /api/leaderboard */
export interface LeaderboardEntry {
    rank: number
    playerId: string
    name: string
    rating: number
    matchesPlayed: number
    highestRating: number
}

/** League as returned by GET /api/leagues (user's league list) */
export interface League {
    id: string
    name: string
    sport: string
    maxTeams: number
    location: string | null
    description: string | null
    cost?: number
    allowRatings: boolean
    isPublic: boolean
    postcode: string | null
    role: string
    pendingJoinRequests: number
}

/** League detail as returned by GET /api/leagues/:id */
export interface LeagueDetail {
    id: string
    name: string
    sport: string
    maxTeams: number
    location: string | null
    description: string | null
    cost: number
    allowRatings: boolean
    isPublic: boolean
    postcode: string | null
    createdDate: string
}

/** League member as returned by GET /api/leagues/:id/members */
export interface LeagueMember {
    userId: string
    email: string
    role: string
    joinedDate: string
}

/** Join request as returned by GET /api/leagues/:id/join-requests */
export interface LeagueJoinRequest {
    id: string
    userId: string
    email: string
    requestedDate: string
    fullName: string
}

/** Search result as returned by GET /api/leagues/search */
export interface LeagueSearchResult {
    id: string
    name: string
    sport: string
    maxTeams: number
    location: string | null
    description: string | null
    cost: number
    postcode: string | null
    memberCount: number
    isAlreadyMember: boolean
    hasPendingRequest: boolean
    lastMatchDate: string | null
    distanceMiles: number
}

/** Public league as returned by GET /api/leagues/:id/public */
export interface PublicLeague {
    id: string
    name: string
    sport: string
    maxTeams: number
    location: string | null
    description: string | null
    cost: number
    postcode: string | null
    createdDate: string
    memberCount: number
    isAlreadyMember: boolean
    hasPendingRequest: boolean
}

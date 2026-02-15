export interface AuthUser {
    email: string
    roles: string[]
    playerId: string | null
    isSuperAdmin: boolean
}

export interface AuthSessionData {
    user: AuthUser
}

export const useSportPositions = () => {
    const getPositionsForSport = (sport: string): string[] => {
        const positionMap: Record<string, string[]> = {
            'Football': ['Goalkeeper', 'Defender', 'Midfielder', 'Forward'],
            'Netball': ['GK', 'GD', 'WD', 'C', 'WA', 'GA', 'GS'],
            'Basketball': ['Point Guard', 'Shooting Guard', 'Small Forward', 'Power Forward', 'Center'],
            'Rugby': ['Prop', 'Hooker', 'Lock', 'Flanker', 'Number 8', 'Scrum-half', 'Fly-half', 'Centre', 'Winger', 'Fullback']
        }

        return positionMap[sport] || ['Any']
    }

    return {
        getPositionsForSport
    }
}

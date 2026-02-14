import { h } from 'vue'

const MockIcon = (name: string) => ({
    name,
    render: () => h('div', { 'data-testid': name })
})

export const Trophy = MockIcon('Trophy')
export const ChevronRight = MockIcon('ChevronRight')
export const Activity = MockIcon('Activity')
export const Calendar = MockIcon('Calendar')
export const Star = MockIcon('Star')
export const TrendingUp = MockIcon('TrendingUp')
export const LogIn = MockIcon('LogIn')
export const Mail = MockIcon('Mail')
export const Lock = MockIcon('Lock')
export const ShieldCheck = MockIcon('ShieldCheck')
export const Loader2 = MockIcon('Loader2')

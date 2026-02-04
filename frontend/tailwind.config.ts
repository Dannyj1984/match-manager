import type { Config } from 'tailwindcss'

export default <Partial<Config>>{
    theme: {
        extend: {
            fontFamily: {
                sans: ['Outfit', 'sans-serif'],
            },
            colors: {
                background: '#0a0a0c',
                card: '#16161a',
                primary: {
                    DEFAULT: '#3b82f6',
                    dark: '#2563eb',
                },
                success: '#10b981',
                warning: '#f59e0b',
                danger: '#ef4444',
                slate: {
                    950: '#0a0a0c',
                    900: '#16161a',
                    800: '#1f1f23',
                    700: '#2e2e33',
                }
            },
            backgroundImage: {
                'glass-gradient': 'linear-gradient(135deg, rgba(255, 255, 255, 0.05) 0%, rgba(255, 255, 255, 0.01) 100%)',
            }
        }
    }
}

<script setup lang="ts">
import { Home, Calendar, User, Users, ListCheck, Trophy } from 'lucide-vue-next'
import type { AuthSessionData } from '~/types/auth'

const { data, status } = useAuth()
const { currentLeague } = useLeague()
const route = useRoute()
const isAdmin = computed(() => currentLeague.value?.role === 'Admin')
const isSuperAdmin = computed(() => (data.value as AuthSessionData | null)?.user?.isSuperAdmin === true)

const isActive = (path: string) => {
    if (path === '/') {
        return route.path === '/'
    }
    return route.path.startsWith(path)
}
</script>

<template>
    <!-- Only show on mobile -->
    <nav class="md:hidden fixed bottom-0 left-0 right-0 z-50 safe-area-bottom">
        <div class="bg-slate-900/95 backdrop-blur-xl border-t border-white/10">
            <div class="flex items-center justify-around px-2 py-3">


                <!-- Match -->
                <NuxtLink v-if="status === 'authenticated' && currentLeague && currentLeague.role !== 'SuperAdmin'"
                    to="/match/setup" :class="[
                        'flex flex-col items-center gap-1 px-4 py-2  transition-all',
                        isActive('/match')
                            ? 'text-primary bg-primary/10'
                            : 'text-slate-500 hover:text-slate-300'
                    ]">
                    <Calendar :size="24" :stroke-width="isActive('/match') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">Match</span>
                </NuxtLink>

                <!-- Leaderboard -->
                <NuxtLink v-if="status === 'authenticated' && currentLeague" to="/leaderboard" :class="[
                    'flex flex-col items-center gap-1 px-4 py-2 transition-all',
                    isActive('/leaderboard')
                        ? 'text-primary bg-primary/10'
                        : 'text-slate-500 hover:text-slate-300'
                ]">
                    <Trophy :size="24" :stroke-width="isActive('/leaderboard') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">Rank</span>
                </NuxtLink>


                <!-- Profile -->
                <NuxtLink v-if="status === 'authenticated' && currentLeague && currentLeague.role !== 'SuperAdmin'"
                    to="/profile" :class="[
                        'flex flex-col items-center gap-1 px-4 py-2 transition-all',
                        isActive('/profile')
                            ? 'text-primary bg-primary/10'
                            : 'text-slate-500 hover:text-slate-300'
                    ]">
                    <User :size="24" :stroke-width="isActive('/profile') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">Profile</span>
                </NuxtLink>
                <!-- Leagues (Admin Only) -->
                <NuxtLink v-if="isAdmin && currentLeague && currentLeague.role === 'Admin'" to="/leagues" :class="[
                    'flex flex-col items-center gap-1 px-4 py-2 transition-all',
                    isActive('/leagues')
                        ? 'text-primary bg-primary/10'
                        : 'text-slate-500 hover:text-slate-300'
                ]">
                    <ListCheck :size="24" :stroke-width="isActive('/players') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">Leagues</span>
                </NuxtLink>
            </div>
        </div>
    </nav>
</template>

<style scoped>
/* Safe area for iOS notch/home indicator */
.safe-area-bottom {
    padding-bottom: env(safe-area-inset-bottom);
}
</style>

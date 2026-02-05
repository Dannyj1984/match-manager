<script setup lang="ts">
import { Home, Calendar, User, Users } from 'lucide-vue-next'

const { data, status } = useAuth()
const { currentLeague } = useLeague()
const route = useRoute()
const isAdmin = computed(() => (data.value as any)?.user?.roles?.includes('Admin'))
const isSuperAdmin = computed(() => (data.value as any)?.user?.isSuperAdmin === true)

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
                <!-- Dashboard -->
                <NuxtLink :to="isSuperAdmin ? '/leagues' : '/'" :class="[
                    'flex flex-col items-center gap-1 px-4 py-2 transition-all',
                    isActive(isSuperAdmin ? '/leagues' : '/')
                        ? 'text-primary bg-primary/10'
                        : 'text-slate-500 hover:text-slate-300'
                ]">
                    <Home :size="24" :stroke-width="isActive(isSuperAdmin ? '/leagues' : '/') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">{{ isSuperAdmin ? 'Leagues' : 'Home'
                        }}</span>
                </NuxtLink>

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

                <!-- Players (Admin Only) -->
                <NuxtLink v-if="isAdmin && currentLeague && currentLeague.role !== 'SuperAdmin'" to="/players" :class="[
                    'flex flex-col items-center gap-1 px-4 py-2 transition-all',
                    isActive('/players')
                        ? 'text-primary bg-primary/10'
                        : 'text-slate-500 hover:text-slate-300'
                ]">
                    <Users :size="24" :stroke-width="isActive('/players') ? 2.5 : 2" />
                    <span class="text-[10px] font-bold uppercase tracking-wider">Players</span>
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

<script setup lang="ts">
import '@/assets/css/main.css'
const { status, data, signOut } = useAuth()
const { currentLeague } = useLeague()
</script>

<template>
  <div class="min-h-screen bg-background selection:bg-primary/30">
    <!-- Navigation (Mini) -->
    <nav
      class="sticky top-0 z-50 glass-card bg-background/80 backdrop-blur-xl border-b border-white/5 px-6 py-4 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <NuxtLink to="/"
          class="w-8 h-8 rounded-lg bg-primary shadow-lg shadow-primary/30 flex items-center justify-center text-white">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M12 22C17.5228 22 22 17.5228 22 12C22 6.47715 17.5228 2 12 2C6.47715 2 2 6.47715 2 12C2 17.5228 6.47715 22 12 22Z"
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M12 12V2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M12 12L7.5 20.5" stroke="currentColor" stroke-width="2" stroke-linecap="round"
              stroke-linejoin="round" />
            <path d="M12 12L16.5 20.5" stroke="currentColor" stroke-width="2" stroke-linecap="round"
              stroke-linejoin="round" />
          </svg>
        </NuxtLink>
        <NuxtLink to="/" class="font-bold tracking-tighter text-lg">FAIRPLAY</NuxtLink>
      </div>

      <div class="hidden md:flex items-center gap-6">
        <NuxtLink :to="(data as any)?.user?.isSuperAdmin ? '/leagues' : '/'"
          class="text-sm font-medium text-slate-400 hover:text-white transition-colors">
          {{ (data as any)?.user?.isSuperAdmin ? 'Leagues' : 'Dashboard' }}
        </NuxtLink>
        <NuxtLink v-if="status === 'authenticated' && currentLeague && currentLeague.role !== 'SuperAdmin'"
          to="/match/setup" class="text-sm font-medium text-slate-400 hover:text-white transition-colors">
          {{ (data as any)?.user?.roles?.includes('Admin') ? 'Match Setup' : 'Matches' }}
        </NuxtLink>
        <NuxtLink v-if="status === 'authenticated' && currentLeague && currentLeague.role !== 'SuperAdmin'"
          to="/profile" class="text-sm font-medium text-slate-400 hover:text-white transition-colors">Profile
        </NuxtLink>
        <NuxtLink
          v-if="status === 'authenticated' && (data as any)?.user?.roles?.includes('Admin') && currentLeague && currentLeague.role !== 'SuperAdmin'"
          to="/players" class="text-sm font-medium text-slate-400 hover:text-white transition-colors">Players</NuxtLink>
      </div>

      <div v-if="status === 'authenticated' && !(data as any)?.user?.isSuperAdmin" class="flex items-center gap-6">
        <LeagueSelector />
      </div>

      <div class="flex items-center gap-4">
        <div v-if="status === 'authenticated'" class="flex items-center gap-4">
          <div class="hidden sm:block text-right">
            <div class="text-[10px] font-bold text-slate-500 uppercase tracking-widest">User</div>
            <div class="text-xs font-medium text-slate-300">{{ (data as any)?.user?.email }}</div>
          </div>
          <button @click="signOut({ callbackUrl: '/login' })"
            class="w-8 h-8 rounded-full bg-slate-800 border border-white/10 hover:border-danger/50 transition-colors flex items-center justify-center group">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
              stroke-linecap="round" stroke-linejoin="round" class="text-slate-500 group-hover:text-danger">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
              <polyline points="16 17 21 12 16 7" />
              <line x1="21" y1="12" x2="9" y2="12" />
            </svg>
          </button>
        </div>
        <NuxtLink v-else to="/login" class="btn-primary py-2 px-4 text-xs">LOG IN</NuxtLink>
      </div>
    </nav>

    <main class="pb-20 md:pb-0">
      <NuxtPage />
    </main>

    <!-- Mobile Bottom Navigation -->
    <BottomNav />

    <!-- Background Decoration -->
    <div class="fixed inset-0 pointer-events-none -z-10 overflow-hidden">
      <div class="absolute -top-[20%] -left-[10%] w-[50%] h-[50%] bg-primary/10 blur-[120px] rounded-full"></div>
      <div class="absolute -bottom-[20%] -right-[10%] w-[40%] h-[40%] bg-blue-600/5 blur-[100px] rounded-full"></div>
    </div>
  </div>
</template>

<style>
@import url('https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700;800;900&display=swap');
</style>

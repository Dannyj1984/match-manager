<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { Play, Users, Trophy, ChevronRight, Activity, Calendar, Star, TrendingUp } from 'lucide-vue-next'

definePageMeta({
  middleware: ['league']
})

const store = useMatchStore()
const { status, data } = useAuth()
const dashboardData = ref<{
  lastCompletedMatchDate: string,
  nextActiveMatchDate: string,
  pendingRatingMatchDate?: string,
  recentPerformance: { date: string, value: number }[]
  needsLeague?: boolean
}>({
  lastCompletedMatchDate: '',
  nextActiveMatchDate: '',
  recentPerformance: []
})
const isLoading = ref(false)

const { currentLeague } = useLeague()

watchEffect(() => {
  if (status.value === 'authenticated' && (data.value as any)?.user?.isSuperAdmin === true) {
    navigateTo('/leagues')
  }
})

watch(currentLeague, async (newLeague) => {
  if (status.value === 'authenticated' && newLeague) {
    isLoading.value = true
    dashboardData.value = await store.fetchDashboard()
    isLoading.value = false
  }
}, { immediate: true })
</script>

<template>
  <div class="min-h-screen bg-[#07090f]">
    <div class="container mx-auto px-4 py-12">
      <!-- Hero / Welcome -->
      <div class="mb-12">
        <h1 class="text-5xl md:text-7xl font-black text-white tracking-tighter mb-4 italic uppercase">
          WELCOME BACK
        </h1>
        <p class="text-slate-400 text-lg font-medium max-w-2xl border-l-2 border-primary/30 pl-6">
          Analyse your stats, view upcoming fixtures, and rate today's top performers.
        </p>
      </div>

      <div v-if="status === 'authenticated'" class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Main Actions -->
        <div class="lg:col-span-2 space-y-8">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <!-- Most Recent Match -->
            <NuxtLink v-if="dashboardData?.lastCompletedMatchDate"
              :to="`/match/setup?date=${dashboardData.lastCompletedMatchDate}`"
              class="group relative overflow-hidden bg-slate-900 border border-white/5 rounded-3xl p-8 hover:border-primary/30 transition-all duration-500">
              <div
                class="absolute -right-4 -bottom-4 opacity-5 group-hover:scale-110 transition-transform duration-700">
                <Trophy :size="160" />
              </div>
              <div class="relative z-10 flex flex-col h-full">
                <div class="w-12 h-12 rounded-2xl bg-success/20 flex items-center justify-center text-success mb-6">
                  <Trophy :size="24" />
                </div>
                <h3 class="text-2xl font-black text-white mb-2 uppercase tracking-tight">Recent Results</h3>
                <p class="text-slate-400 text-sm font-medium mb-8">View the outcome and player ratings from <span
                    class="text-white">{{ formatDate(dashboardData.lastCompletedMatchDate) }}</span></p>
                <div class="mt-auto flex items-center text-primary font-black text-xs uppercase tracking-widest gap-2">
                  View Match
                  <ChevronRight :size="14" />
                </div>
              </div>
            </NuxtLink>

            <!-- No Recent Match State -->
            <div v-else
              class="bg-slate-900/40 border border-dashed border-white/10 rounded-3xl p-8 flex flex-col items-center justify-center text-center">
              <Trophy :size="32" class="text-slate-700 mb-4" />
              <p class="text-slate-500 text-sm font-bold uppercase tracking-widest">No recent matches</p>
            </div>

            <!-- Next Active Match -->
            <NuxtLink v-if="dashboardData?.nextActiveMatchDate"
              :to="`/match/setup?date=${dashboardData.nextActiveMatchDate}`"
              class="group relative overflow-hidden bg-slate-900 border border-white/5 rounded-3xl p-8 hover:border-primary/30 transition-all duration-500">
              <div
                class="absolute -right-4 -bottom-4 opacity-5 group-hover:scale-110 transition-transform duration-700">
                <Calendar :size="160" />
              </div>
              <div class="relative z-10 flex flex-col h-full">
                <div class="w-12 h-12 rounded-2xl bg-primary/20 flex items-center justify-center text-primary mb-6">
                  <Activity :size="24" />
                </div>
                <h3 class="text-2xl font-black text-white mb-2 uppercase tracking-tight">Active Match</h3>
                <p class="text-slate-400 text-sm font-medium mb-8">Check the teams and schedule for <span
                    class="text-white">{{ formatDate(dashboardData.nextActiveMatchDate) }}</span></p>
                <div class="mt-auto flex items-center text-primary font-black text-xs uppercase tracking-widest gap-2">
                  Join Match
                  <ChevronRight :size="14" />
                </div>
              </div>
            </NuxtLink>

            <!-- No Active Match State -->
            <div v-else
              class="bg-slate-900/40 border border-dashed border-white/10 rounded-3xl p-8 flex flex-col items-center justify-center text-center text-slate-500">
              <Calendar :size="32" class="mb-4 opacity-20" />
              <p class="text-xs font-black uppercase tracking-widest">No upcoming matches</p>
            </div>
          </div>

          <!-- Performance Stats Section -->
          <div class="bg-slate-900 border border-white/5 rounded-3xl p-8 overflow-hidden relative">
            <div class="flex items-center justify-between mb-8">
              <div>
                <h2 class="text-xl font-black text-white uppercase tracking-tight">Recent Performance</h2>
                <p class="text-xs text-slate-500 font-bold uppercase tracking-widest mt-1">Last 4 matches</p>
              </div>
              <TrendingUp class="text-primary/30" :size="32" />
            </div>

            <div v-if="dashboardData?.recentPerformance?.length > 0" class="flex items-end gap-4 h-32">
              <div v-for="(perf, idx) in dashboardData?.recentPerformance" :key="idx"
                class="flex-1 flex flex-col items-center gap-3 group relative">
                <div
                  class="w-full bg-slate-950 rounded-t-xl relative overflow-hidden group-hover:bg-slate-800 transition-colors"
                  :style="{ height: (perf.value * 10) + '%' }">
                  <div class="absolute inset-0 bg-gradient-to-t from-primary/20 to-transparent"></div>
                </div>
                <div class="text-[10px] font-black text-slate-500 uppercase">{{ formatDate(perf.date).split(',')[1] }}
                </div>
                <div
                  class="absolute -top-10 left-1/2 -translate-x-1/2 opacity-0 opacity-100 transition-all bg-primary text-[10px] font-black px-3 py-1.5 rounded-lg shadow-xl shadow-primary/40 text-white z-20 whitespace-nowrap">
                  RATING: {{ perf.value }}
                </div>
              </div>
            </div>
            <div v-else class="py-12 flex flex-col items-center justify-center text-center opacity-30">
              <Activity :size="32" class="mb-2" />
              <p class="text-xs font-bold uppercase tracking-widest">Statistical data pending</p>
            </div>
          </div>
        </div>

        <!-- Side Info -->
        <div class="space-y-8">
          <div
            class="bg-primary/5 border border-primary/20 rounded-3xl p-8 relative overflow-hidden group hover:bg-primary/10 transition-colors">
            <h3 class="text-lg font-black text-white mb-2 italic uppercase">COMMUNITY STANDINGS</h3>
            <p class="text-slate-400 text-sm font-medium mb-6 leading-relaxed">See all players.</p>
            <NuxtLink to="/players"
              class="inline-flex items-center gap-2 text-primary font-black text-xs uppercase tracking-widest hover:translate-x-1 transition-transform">
              All Players
              <ChevronRight :size="14" />
            </NuxtLink>
          </div>

          <div v-if="dashboardData?.pendingRatingMatchDate" class="bg-slate-900 border border-white/5 rounded-3xl p-8">
            <div class="flex items-center gap-3 mb-6">
              <div class="w-8 h-8 rounded-lg bg-primary/20 flex items-center justify-center text-primary">
                <Star :size="16" fill="currentColor" />
              </div>
              <h3 class="text-sm font-black text-white uppercase tracking-widest">Rate Now</h3>
            </div>
            <p class="text-slate-400 text-xs font-medium leading-relaxed mb-6">Help keep the community fair. Rate
              players from your recent match on <span class="text-white">{{
                formatDate(dashboardData.pendingRatingMatchDate) }}</span>.</p>
            <NuxtLink :to="`/match/setup?date=${dashboardData.pendingRatingMatchDate}`"
              class="block w-full text-center bg-white/5 hover:bg-white/10 text-white font-black text-[10px] uppercase tracking-[0.2em] py-4 rounded-xl transition-colors border border-white/5">
              Submit Ratings
            </NuxtLink>
          </div>
        </div>
      </div>

      <!-- Anonymous View -->
      <div v-else
        class="text-center py-24 bg-slate-900/40 border border-white/5 rounded-[3.5rem] relative overflow-hidden group">
        <div class="absolute inset-0 bg-gradient-to-b from-primary/5 to-transparent opacity-50"></div>

        <div class="relative z-10">
          <div class="mb-10 flex justify-center">
            <div
              class="w-20 h-20 rounded-[2.5rem] bg-primary/10 flex items-center justify-center text-primary animate-pulse group-hover:scale-110 transition-transform duration-500">
              <Activity :size="40" />
            </div>
          </div>
          <h2 class="text-4xl md:text-5xl font-black text-white mb-6 italic tracking-tighter uppercase">READY TO JOIN
            THE GAME?</h2>
          <p class="text-slate-400 text-lg font-medium mb-10 max-w-lg mx-auto leading-relaxed">Login to access your
            personalised dashboard, view match details, and start rating players.</p>
          <NuxtLink to="/login"
            class="inline-flex items-center bg-primary text-white px-12 py-5 rounded-[2rem] font-black text-sm uppercase tracking-widest shadow-2xl shadow-primary/30 hover:scale-105 active:scale-95 transition-all">
            Get Started
            <ChevronRight class="ml-2" :size="18" />
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.container {
  max-width: 1200px;
}
</style>

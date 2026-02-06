<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { Users, Wand2, Save, Calendar as CalendarIcon, CheckCircle2, Star } from 'lucide-vue-next'

const store = useMatchStore()
const modal = useModal()

const route = useRoute()

const { currentLeague } = useLeague()
const isAdmin = computed(() => currentLeague.value?.role === 'Admin')

watch(currentLeague, async (newLeague) => {
  if (newLeague) {
    await store.fetchPlayers()

    // Fetch current user's player ID
    const me = await store.fetchMe()
    if (me) {
      store.currentPlayerId = me.id
    }

    if (route.query.date) {
      store.matchDate = route.query.date as string
    }

    // Initial fetch for selected date
    await store.fetchMatchByDate(store.matchDate)
  }
}, { immediate: true })

const handleGenerateTeams = async () => {
  await store.generateTeams()
}

const handleSaveMatch = async () => {
  const result = await store.saveMatch()
  if (result.success) {
    modal.showInfo('Match saved successfully!', 'Success')
  } else {
    modal.showError(result.error, 'Error')
  }
}

const onDateChange = async () => {
  await store.fetchMatchByDate(store.matchDate)
}

const showRatingModal = ref(false)
const showPlayerList = ref(false)

const formatDate = (dateStr: string) => {
  const options: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'long', day: 'numeric' }
  return new Date(dateStr).toLocaleDateString(undefined, options)
}

const handleCompleteMatch = async () => {
  const matchId = store.currentMatchId
  if (!matchId) return
  const confirmed = await modal.showConfirm(
    `Are you sure you want to complete this match? Ratings will be locked and player averages updated.`,
    'Complete Match'
  )
  if (!confirmed) return

  const result = await store.completeMatch(matchId)
  if (result.success) {
    modal.showInfo('Match completed successfully!', 'Success')
    await store.fetchMatchByDate(store.matchDate)
  } else {
    modal.showError(result.error, 'Error')
  }
}

const isParticipating = computed(() => {
  return store.selectedPlayerIds.includes(store.currentPlayerId || '')
})

const handleToggleParticipation = async () => {
  const newState = !isParticipating.value
  const result = await store.toggleSelfParticipation(store.matchDate, newState, store.currentMatchId || undefined, store.formatType)

  if (result.success) {
    modal.showSuccess(result.message || (newState ? "You're in!" : "You're out"))
  } else {
    modal.showError(result.error || 'Failed to update participation')
  }
}

definePageMeta({
  middleware: ['auth', 'league', 'member']
})
</script>

<template>
  <div class="max-w-[1600px] mx-auto px-4 py-8 md:py-12 space-y-8">
    <!-- Header Section -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
      <div class="space-y-2">
        <div class="flex items-center gap-2 text-primary font-bold text-sm uppercase tracking-widest">
          <div class="w-1.5 h-1.5 rounded-full bg-primary animate-pulse"></div>
          {{ isAdmin ? 'Match Setup' : 'Match Archive' }}
        </div>
        <h1 class="text-4xl md:text-5xl font-black text-white tracking-tight">FAIRPLAY <span class="text-slate-500">{{
          isAdmin ? 'DRAFT' : 'RESULTS' }}</span></h1>
      </div>

      <!-- Main Config Bar -->
      <div class="glass-card p-4 flex flex-wrap items-center gap-6 border-white/5">
        <div class="space-y-1.5">
          <label class="text-[10px] font-black text-slate-500 uppercase tracking-widest px-1">Match Date</label>
          <div class="relative group">
            <CalendarIcon :size="14"
              class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-primary transition-colors" />
            <input v-model="store.matchDate" type="date" @change="onDateChange"
              class="bg-slate-900/50 border border-white/5 rounded-xl py-2 pl-10 pr-4 text-xs font-bold text-white focus:outline-none focus:border-primary/50 transition-all cursor-pointer">
          </div>
        </div>

        <!-- Member Self-Selection Toggle -->
        <div v-if="!isAdmin && !store.isCompleted" class="space-y-1.5">
          <label class="text-[10px] font-black text-slate-500 uppercase tracking-widest px-1">My Status</label>
          <button @click="handleToggleParticipation" :class="[
            'flex items-center gap-2 px-5 py-2.5 rounded-xl font-black text-xs transition-all shadow-lg active:scale-95 whitespace-nowrap',
            isParticipating
              ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-emerald-500/20 border border-emerald-400/20'
              : 'bg-slate-800 hover:bg-slate-700 text-slate-300 border border-white/5'
          ]">
            <CheckCircle2 v-if="isParticipating" :size="16" />
            <Users v-else :size="16" />
            {{ isParticipating ? "I'M PLAYING ✓" : "I'M NOT PLAYING" }}
          </button>
        </div>

        <div v-if="isAdmin" class="space-y-1.5 flex flex-col">
          <label class="text-[10px] font-black text-slate-500 uppercase tracking-widest px-1">Team Count</label>
          <select v-model="store.teamsCount" @change="store.updateFormatType()"
            class="bg-slate-900/50 border border-white/5 rounded-xl py-2 px-4 text-xs font-bold text-white focus:outline-none focus:border-primary/50 transition-all cursor-pointer min-w-[100px]">
            <option :value="2">2 Teams</option>
            <option :value="3">3 Teams</option>
            <option :value="4">4 Teams</option>
          </select>
        </div>

        <div v-if="isAdmin" class="space-y-1.5 flex flex-col">
          <label class="text-[10px] font-black text-slate-500 uppercase tracking-widest px-1">Max Players</label>
          <input type="number" v-model.number="store.maxPlayers" @input="store.updateFormatType()" min="4" max="50"
            step="1"
            class="bg-slate-900/50 border border-white/5 rounded-xl py-2 px-4 text-xs font-bold text-white focus:outline-none focus:border-primary/50 transition-all w-20" />
        </div>

        <div v-if="isAdmin" class="hidden md:flex flex-col items-center justify-center px-2">
          <span class="text-[10px] font-black text-slate-500 uppercase tracking-widest">Format</span>
          <span class="text-xs font-black text-primary">{{ store.formatType }}</span>
        </div>

        <div v-if="isAdmin" class="h-10 w-px bg-white/5 hidden md:block"></div>

        <div v-if="isAdmin" class="flex items-center gap-2 flex-wrap">
          <button v-if="!store.isCompleted" @click="handleGenerateTeams"
            :disabled="store.selectedPlayerIds.length === 0 || store.isLoading"
            class="flex items-center gap-2 bg-primary hover:bg-primary-dark disabled:opacity-50 text-white px-5 py-2.5 rounded-xl font-black text-xs transition-all shadow-lg shadow-primary/20 active:scale-95 whitespace-nowrap">
            <Wand2 :size="16" :class="{ 'animate-spin': store.isLoading }" />
            GENERATE TEAMS
          </button>
          <button v-if="!store.isCompleted" @click="handleSaveMatch" :disabled="store.isLoading"
            class="flex items-center gap-2 bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl font-black text-xs transition-all border border-white/5 active:scale-95 whitespace-nowrap">
            <Save :size="16" />
            SAVE MATCH
          </button>

          <button v-if="store.currentMatchId && !store.isCompleted" @click="handleCompleteMatch"
            :disabled="store.isLoading"
            class="flex items-center gap-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white px-5 py-2.5 rounded-xl font-black text-xs transition-all shadow-lg shadow-emerald-500/20 active:scale-95 whitespace-nowrap border border-emerald-400/20">
            <CheckCircle2 :size="16" />
            COMPLETE MATCH
          </button>
        </div>

        <div v-if="store.isCompleted && store.canRate" class="flex items-center gap-2">
          <button @click="showRatingModal = true"
            class="flex items-center gap-2 bg-primary hover:bg-primary-dark text-white px-5 py-2.5 rounded-xl font-black text-xs transition-all shadow-lg shadow-primary/20 active:scale-95 whitespace-nowrap">
            <Star :size="16" />
            RATE PLAYERS
          </button>
        </div>

        <div v-if="store.isCompleted"
          class="px-4 py-2 bg-emerald-500/10 border border-emerald-500/20 text-emerald-500 rounded-xl flex items-center gap-2 text-[10px] font-black uppercase tracking-widest ml-auto">
          <div class="w-1.5 h-1.5 rounded-full bg-emerald-500"></div>
          Completed
        </div>
      </div>
    </div>

    <!-- Main Layout -->
    <div class="grid grid-cols-1 xl:grid-cols-4 gap-8">
      <!-- Sidebar: The Pot -->
      <div v-if="isAdmin || store.isCompleted" class="xl:col-span-1 border-r border-white/5 pr-0 xl:pr-8 h-[700px]">
        <PlayerSelectSidebar />
      </div>

      <!-- Teams Area -->
      <div :class="(isAdmin || store.isCompleted) ? 'xl:col-span-3' : 'xl:col-span-4'" class="space-y-8">
        <!-- Stats Bar -->
        <div v-if="!store.isCompleted" class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- In Pot Card - Now Collapsible -->
          <div class="glass-card p-4 border-l-4 border-l-primary/50 col-span-2 lg:col-span-2">
            <button @click="showPlayerList = !showPlayerList"
              class="w-full flex items-center justify-between text-left group hover:opacity-80 transition-opacity">
              <div class="text-xs font-bold text-slate-500 uppercase tracking-widest">Players In Match</div>
              <div class="flex items-center gap-2">
                <div class="text-2xl font-black text-white">{{ store.selectedPlayerIds.length }}</div>
                <svg :class="['w-4 h-4 text-slate-500 transition-transform', showPlayerList ? 'rotate-180' : '']"
                  fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </button>

            <!-- Collapsible Player List -->
            <Transition name="expand">
              <div v-if="showPlayerList" class="mt-4 pt-4 border-t border-white/5 space-y-2 max-h-60 overflow-y-auto">
                <div v-for="playerId in store.selectedPlayerIds" :key="playerId" :class="[
                  'flex items-center justify-between text-xs py-1.5 px-2 rounded-lg transition-colors',
                  playerId === store.currentPlayerId
                    ? 'bg-emerald-600/20 border border-emerald-500/30 hover:bg-emerald-600/30'
                    : 'bg-white/5 hover:bg-white/10'
                ]">
                  <div class="flex items-center gap-2">
                    <Users :size="12"
                      :class="playerId === store.currentPlayerId ? 'text-emerald-400' : 'text-primary'" />
                    <span
                      :class="playerId === store.currentPlayerId ? 'text-emerald-300 font-bold' : 'text-slate-300 font-medium'">{{
                        store.availablePlayers.find(p => p.id === playerId)?.fullName || 'Unknown'
                      }}</span>
                    <span v-if="playerId === store.currentPlayerId"
                      class="text-[10px] text-emerald-400 font-black uppercase">(You)</span>
                  </div>
                  <div class="flex items-center gap-3">
                    <!-- Average Match Rating -->
                    <div v-if="store.availablePlayers.find(p => p.id === playerId)?.avgMatchRating"
                      class="flex items-center gap-1 text-emerald-400" title="Avg Match Rating (Last 6 Months)">
                      <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M3 3v18h18" />
                        <path d="M18 17V9" />
                        <path d="M13 17V5" />
                        <path d="M8 17v-3" />
                      </svg>
                      <span class="font-bold">{{
                        store.availablePlayers.find(p => p.id === playerId)?.avgMatchRating?.toFixed(1)
                      }}</span>
                    </div>

                    <!-- Ability Rating -->
                    <div class="flex items-center gap-1 text-yellow-500" title="Player Ability">
                      <Star :size="12" :fill="'currentColor'" />
                      <span class="font-bold">{{
                        store.availablePlayers.find(p => p.id === playerId)?.currentRating || '?'
                      }}</span>
                    </div>
                  </div>
                </div>
                <div v-if="store.selectedPlayerIds.length === 0" class="text-xs text-slate-500 italic py-2">
                  No players signed up yet
                </div>
              </div>
            </Transition>
          </div>

          <div class="glass-card p-4 flex items-center justify-between border-l-4 border-l-warning/50">
            <div class="text-xs font-bold text-slate-500 uppercase tracking-widest">Balance Diff</div>
            <div class="text-2xl font-black text-white">{{ store.powerBalance }}</div>
          </div>
        </div>

        <!-- Scrollable Teams Grid -->
        <div v-if="Object.values(store.teams).some(players => players.length > 0)"
          class="grid grid-cols-1 lg:grid-cols-2 gap-8 pb-20">
          <div v-for="n in store.teamsCount" :key="n">
            <TeamColumn :title="`Team ${String.fromCharCode(64 + n)}`" :team-number="n" v-model:players="store.teams[n]"
              :power-score="store.teamPower(n).toFixed(1)" />
          </div>
        </div>

        <!-- No Match State -->
        <div v-else-if="!store.matchDate"
          class="flex flex-col items-center justify-center py-20 bg-white/5 border border-dashed border-white/10 rounded-3xl">
          <CalendarIcon :size="48" class="text-slate-700 mb-4" />
          <h3 class="text-xl font-bold text-slate-400">No match scheduled</h3>
          <p class="text-slate-500 text-sm mb-6">Select another date to view previous matches.</p>

          <div v-if="store.nextMatchDate"
            class="space-y-4 text-center animate-in fade-in slide-in-from-bottom-4 duration-500">
            <div
              class="px-4 py-2 bg-primary/10 border border-primary/20 rounded-full text-primary text-[10px] font-black uppercase tracking-widest inline-block">
              Next Match Found
            </div>
            <p class="text-white font-medium">There is a match scheduled for <span class="text-primary font-black">{{
              formatDate(store.nextMatchDate) }}</span></p>
            <button @click="store.matchDate = store.nextMatchDate; onDateChange()"
              class="btn-primary px-6 py-2 text-xs uppercase">
              JUMP TO {{ formatDate(store.nextMatchDate) }}
            </button>
          </div>
        </div>
        <div v-else
          class="flex flex-col items-center justify-center py-20 bg-white/5 border border-dashed border-white/10 rounded-3xl">
          <CalendarIcon :size="48" class="text-slate-700 mb-4" />
          <h3 class="text-xl font-bold text-slate-400">Match scheduled</h3>
          <p class="text-slate-500 text-sm mb-6">Teams will be available shortly</p>


        </div>
      </div>
    </div>


    <PlayerRatingModal v-if="showRatingModal" v-model="showRatingModal" :match-id="store.currentMatchId!"
      :teams="store.teams" :current-player-id="store.currentPlayerId || ''" />
  </div>
</template>

<style scoped>
.font-display {
  font-family: 'Outfit', sans-serif;
}

.expand-enter-active,
.expand-leave-active {
  transition: all 0.3s ease;
  max-height: 300px;
  overflow: hidden;
}

.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}
</style>

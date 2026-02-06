<script setup lang="ts">
import { Trophy, Calendar, Activity } from 'lucide-vue-next'
import { useMatchStore } from '@/stores/match'

const store = useMatchStore()

interface Player {
  id: string
  fullName: string
  currentRating: number
  avgMatchRating?: number
  preferredPosition: string
  lastPlayed?: string
}

defineProps<{
  player: Player
}>()
</script>

<template>
  <div
    class="glass border border-white/10 p-4 rounded-xl flex items-center justify-between group hover:border-primary/30 transition-all duration-300"
    :class="store.isCompleted ? 'cursor-default' : 'cursor-grab active:cursor-grabbing'">
    <div class="flex items-center gap-3">
      <div
        class="w-10 h-10 rounded-full bg-slate-800 flex items-center justify-center text-primary font-bold border border-white/5">
        {{ player.fullName.charAt(0).toUpperCase() }}
      </div>
      <div>
        <div class="flex items-center gap-2">
          <h3 class="font-medium text-slate-100 group-hover:text-primary transition-colors leading-none">{{
            player.fullName }}</h3>
          <span
            class="text-[9px] font-black bg-white/5 text-slate-500 border border-white/5 py-0.5 px-1.5 rounded uppercase tracking-tighter">{{
              player.preferredPosition }}</span>
        </div>
        <p v-if="player.lastPlayed" class="text-[10px] text-slate-500 flex items-center gap-1 mt-1">
          <Calendar :size="10" /> {{ new Date(player.lastPlayed).toLocaleDateString() }}
        </p>
      </div>
    </div>

    <div class="flex items-center gap-2">
      <!-- Avg Match Rating -->
      <div v-if="player.avgMatchRating"
        class="px-2 py-1 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center gap-1.5"
        title="Avg Match Rating (Last 6 Months)">
        <Activity :size="12" class="text-emerald-400" />
        <span class="text-xs font-bold text-emerald-400">{{ Number(player.avgMatchRating).toFixed(1) }}</span>
      </div>

      <!-- Ability Rating -->
      <div class="px-2 py-1 rounded-lg bg-white/5 border border-white/5 flex items-center gap-1.5 shadow-inner"
        title="Ability Rating">
        <Trophy :size="12" class="text-warning" />
        <span class="text-xs font-bold text-slate-200">{{ Number(player.currentRating).toFixed(1) }}</span>
      </div>
    </div>
  </div>
</template>

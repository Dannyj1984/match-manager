<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import draggable from 'vuedraggable'
import { ShieldCheck, Zap } from 'lucide-vue-next'
import type { Player } from '~/types/player'

const store = useMatchStore()
const props = defineProps<{
  title: string
  teamNumber: number
  players: Player[]
  powerScore: string | number
}>()

const emit = defineEmits(['update:players'])

const list = computed({
  get: () => props.players,
  set: (val) => emit('update:players', val)
})
const getTeamColor = (num: number) => {
  const colors = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#ec4899']
  return colors[(num - 1) % colors.length]
}

const teamColor = computed(() => getTeamColor(props.teamNumber))
</script>

<template>
  <div class="flex flex-col gap-4 h-full">
    <div class="flex items-center justify-between px-2">
      <div class="flex items-center gap-2">
        <div :style="{ backgroundColor: `${teamColor}22`, color: teamColor }"
          class="w-8 h-8 rounded-lg flex items-center justify-center shadow-lg border border-white/5">
          <ShieldCheck :size="18" />
        </div>
        <h2 class="text-xl font-bold font-display uppercase tracking-wider text-slate-200">{{ title }}</h2>
      </div>

      <div class="flex items-center gap-2 bg-slate-900/50 px-3 py-1.5 rounded-full border border-white/5">
        <Zap :size="14" class="text-warning fill-warning" />
        <span class="text-xs font-bold font-mono text-slate-300">PWR: {{ powerScore }}</span>
      </div>
    </div>

    <draggable v-model="list" group="players" item-key="id" :disabled="store.isCompleted"
      class="flex-1 space-y-3 min-h-[300px] p-4 bg-slate-900/30 rounded-3xl border-2 border-dashed border-white/5 hover:border-white/10 transition-colors"
      ghost-class="opacity-50" animation="200">
      <template #item="{ element }">
        <PlayerCard :player="element" />
      </template>
    </draggable>
  </div>
</template>

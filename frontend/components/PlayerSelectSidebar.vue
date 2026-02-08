<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { Search, UserPlus, UserMinus, X } from 'lucide-vue-next'

const store = useMatchStore()
const searchQuery = ref('')

const filteredPlayers = computed(() => {
    let players = store.availablePlayers

    // If match is complete, only show players who were part of the match
    if (store.isCompleted) {
        players = players.filter(p => store.selectedPlayerIds.includes(p.id))
    }

    if (!searchQuery.value) return players
    const query = searchQuery.value.toLowerCase()
    return players.filter(p =>
        p.fullName.toLowerCase().includes(query)
    )
})

const isSelected = (id: string) => store.selectedPlayerIds.includes(id)

const togglePlayer = (id: string) => {
    if (store.isCompleted) return

    const index = store.selectedPlayerIds.indexOf(id)
    if (index > -1) {
        store.selectedPlayerIds.splice(index, 1)
    } else {
        store.selectedPlayerIds.push(id)
    }
}

const clearSearch = () => {
    searchQuery.value = ''
}
</script>

<template>
    <div class="glass-card flex flex-col h-full overflow-hidden border-white/5">
        <div class="p-4 border-b border-white/5 space-y-4">
            <div class="flex items-center justify-between">
                <h3 class="text-sm font-black text-slate-400 uppercase tracking-widest">Selected Players</h3>
                <span class="bg-primary/20 text-primary text-[10px] font-black px-2 py-0.5 rounded-full">
                    {{ store.selectedPlayerIds.length }} SELECTED
                </span>
            </div>

            <div class="relative group">
                <Search :size="16"
                    class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-primary transition-colors" />
                <input v-model="searchQuery" type="text" placeholder="Search players..."
                    class="w-full bg-slate-900/50 border border-white/5 rounded-xl py-2.5 pl-10 pr-10 text-sm text-white focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/20 transition-all">
                <button v-if="searchQuery" @click="clearSearch"
                    class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white">
                    <X :size="14" />
                </button>
            </div>
        </div>

        <div class="flex-1 overflow-y-auto p-2 custom-scrollbar">
            <div v-if="filteredPlayers.length === 0"
                class="flex flex-col items-center justify-center py-12 text-center px-4">
                <div
                    class="w-12 h-12 rounded-2xl bg-slate-900 flex items-center justify-center mb-4 border border-white/5">
                    <Search :size="20" class="text-slate-600" />
                </div>
                <p class="text-slate-400 text-sm font-medium">No players found</p>
                <p class="text-slate-500 text-xs mt-1">Try another search term</p>
            </div>

            <div v-else class="space-y-1">
                <div v-for="player in filteredPlayers" :key="player.id" @click="togglePlayer(player.id)" :class="[
                    'group flex items-center justify-between p-3 rounded-xl transition-all border',
                    isSelected(player.id)
                        ? 'bg-primary/10 border-primary/20'
                        : 'hover:bg-white/5 border-transparent',
                    store.isCompleted ? 'cursor-default' : 'cursor-pointer'
                ]">
                    <div class="flex items-center gap-3">
                        <div :class="[
                            'w-8 h-8 rounded-lg flex items-center justify-center font-bold text-xs',
                            isSelected(player.id) ? 'bg-primary text-white' : 'bg-slate-800 text-slate-400'
                        ]">
                            {{player.fullName.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}}
                        </div>
                        <div>
                            <div class="flex items-center gap-2">
                                <p class="text-sm font-bold text-white leading-none">{{ player.fullName }}</p>
                                <span
                                    class="text-[8px] font-black bg-white/5 text-slate-500 border border-white/5 py-0 px-1 rounded uppercase tracking-tighter">{{
                                        player.preferredPosition.join(', ') }}</span>
                            </div>
                            <p class="text-[10px] text-slate-500 mt-1 font-mono uppercase tracking-wider">Rating: {{
                                player.currentRating }}</p>
                        </div>
                    </div>

                    <div v-if="!store.isCompleted" :class="[
                        'w-7 h-7 rounded-lg flex items-center justify-center transition-all',
                        isSelected(player.id)
                            ? 'bg-primary/20 text-primary'
                            : 'bg-slate-900 text-slate-600 group-hover:text-primary group-hover:bg-primary/10'
                    ]">
                        <UserMinus v-if="isSelected(player.id)" :size="16" />
                        <UserPlus v-else :size="16" />
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
    width: 4px;
}

.custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
}

.custom-scrollbar::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 10px;
}

.custom-scrollbar::-webkit-scrollbar-thumb:hover {
    background: rgba(255, 255, 255, 0.1);
}
</style>

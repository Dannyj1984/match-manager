<script setup lang="ts">
import { Trophy, Medal, Calendar, Award } from 'lucide-vue-next'

definePageMeta({
    middleware: 'auth'
})

const { token } = useAuth()
const { currentLeague } = useLeague()

const timeframe = ref('all')
const leaderboard = ref<any[]>([])
const isLoading = ref(true)

const timeframes = [
    { id: 'all', label: 'All Time' },
    { id: '12m', label: '12 Months' },
    { id: '6m', label: '6 Months' },
    { id: '3m', label: '3 Months' }
]

const fetchLeaderboard = async () => {
    if (!currentLeague.value?.id) return

    isLoading.value = true
    try {
        const data = await $fetch(`/api/leaderboard?timeframe=${timeframe.value}`, {
            headers: {
                Authorization: token.value as string,
                'X-League-Id': currentLeague.value.id
            }
        })
        leaderboard.value = data as any[]
    } catch (error) {
        console.error('Failed to fetch leaderboard:', error)
    } finally {
        isLoading.value = false
    }
}

// Re-fetch when timeframe or league changes
watch([timeframe, () => currentLeague.value?.id], () => {
    fetchLeaderboard()
}, { immediate: true })

const getRankIcon = (rank: number) => {
    if (rank === 1) return { icon: Trophy, color: 'text-yellow-400' }
    if (rank === 2) return { icon: Medal, color: 'text-slate-300' } // Silver
    if (rank === 3) return { icon: Medal, color: 'text-amber-600' } // Bronze
    return null
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-4xl">
        <div class="flex items-center justify-between mb-8">
            <div>
                <h1 class="text-3xl font-bold mb-2 flex items-center gap-3">
                    <Trophy class="text-primary" />
                    Leaderboard
                </h1>
                <p class="text-slate-400">Top rated players based on match performance</p>
            </div>

            <!-- Timeframe Selector (Desktop) -->
            <div class="hidden md:flex bg-slate-800 p-1 rounded-lg">
                <button v-for="tf in timeframes" :key="tf.id" @click="timeframe = tf.id"
                    class="px-4 py-2 rounded-md text-sm font-medium transition-all"
                    :class="timeframe === tf.id ? 'bg-primary text-white shadow-lg' : 'text-slate-400 hover:text-white'">
                    {{ tf.label }}
                </button>
            </div>
        </div>

        <!-- Timeframe Selector (Mobile) -->
        <div class="md:hidden mb-6">
            <select v-model="timeframe"
                class="w-full bg-slate-800 text-white border border-white/10 rounded-lg px-4 py-3 outline-none focus:border-primary">
                <option v-for="tf in timeframes" :key="tf.id" :value="tf.id">{{ tf.label }}</option>
            </select>
        </div>

        <div v-if="isLoading" class="flex justify-center py-12">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
        </div>

        <div v-else-if="leaderboard.length === 0" class="text-center py-12">
            <div
                class="w-16 h-16 bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-4 text-slate-500">
                <Award :size="32" />
            </div>
            <h3 class="text-lg font-medium text-white mb-2">No stats yet</h3>
            <p class="text-slate-400">Play matches and submit ratings to appear on the leaderboard!</p>
            <p class="text-slate-500 text-sm mt-2">(Minimum 3 matches required)</p>
        </div>

        <div v-else class="space-y-4">
            <div v-for="player in leaderboard" :key="player.playerId"
                class="glass-card p-4 flex items-center gap-4 transition-transform hover:scale-[1.01]">

                <!-- Rank -->
                <div class="w-12 h-12 flex-shrink-0 flex items-center justify-center font-black text-xl">
                    <component :is="getRankIcon(player.rank)?.icon" v-if="getRankIcon(player.rank)"
                        :class="getRankIcon(player.rank)?.color" class="w-8 h-8" />
                    <span v-else class="text-slate-500">#{{ player.rank }}</span>
                </div>

                <!-- Player Info -->
                <div class="flex-grow">
                    <div class="font-bold text-lg text-white">{{ player.name }}</div>
                    <div class="text-xs text-slate-400 flex items-center gap-1">
                        <Calendar :size="12" />
                        {{ player.matchesPlayed }} matches played
                    </div>
                </div>

                <!-- Rating -->
                <div class="text-right">
                    <div class="flex justify-between gap-4">
                        <div>
                            <div class="text-2xl font-black text-primary">{{ player.highestRating.toFixed(1) }}</div>
                            <div class="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Highest</div>
                        </div>
                        <div>
                            <div class="text-2xl font-black text-primary">{{ player.rating.toFixed(1) }}</div>
                            <div class="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Avg Rating</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

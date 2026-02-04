<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { X, Star, Save, Loader2, CheckCircle2 } from 'lucide-vue-next'

const props = defineProps<{
    modelValue: boolean
    matchId: string
    teams: Record<number, any[]>
    currentPlayerId: string
}>()

const emit = defineEmits(['update:modelValue', 'success'])

const store = useMatchStore()
const isSaving = ref(false)
const isLoading = ref(true)
const ratings = ref<Record<string, number>>({})
const existingRatings = ref<Record<string, number>>({})
const hasRated = ref(false)

onMounted(async () => {
    isLoading.value = true
    const myRatings = await store.fetchMyRatings(props.matchId)
    if (myRatings && myRatings.length > 0) {
        hasRated.value = true
        myRatings.forEach((r: any) => {
            existingRatings.value[r.subjectId] = r.value
            ratings.value[r.subjectId] = r.value
        })
    } else {
        // Initialize with default 5
        Object.values(props.teams).flat().forEach(p => {
            ratings.value[p.id] = p.currentRating
        })
    }
    isLoading.value = false
})

const handleSave = async () => {
    if (hasRated.value) return

    isSaving.value = true
    const submissions = Object.entries(ratings.value)
        .filter(([id]) => id !== props.currentPlayerId) // Don't submit rating for self
        .map(([id, val]) => ({
            subjectId: id,
            value: Math.round(val) // Ensure integer value
        }))

    const result = await store.submitRatings(props.matchId, submissions)
    if (result.success) {
        hasRated.value = true
        emit('success')
        setTimeout(() => emit('update:modelValue', false), 2000)
    } else {
        alert(result.error)
    }
    isSaving.value = false
}
</script>

<template>
    <div v-if="modelValue" class="fixed inset-0 z-[100] flex items-center justify-center p-4">
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-slate-950/80 backdrop-blur-sm" @click="emit('update:modelValue', false)"></div>

        <!-- Modal -->
        <div
            class="relative w-full max-w-4xl bg-slate-900 border border-white/10 rounded-3xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
            <!-- Header -->
            <div class="px-8 py-6 border-b border-white/5 flex items-center justify-between bg-white/[0.02]">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-primary/20 flex items-center justify-center text-primary">
                        <Star :size="20" fill="currentColor" />
                    </div>
                    <div>
                        <h2 class="text-xl font-bold text-white leading-tight">Rate Ability</h2>
                        <p class="text-xs text-slate-500 font-medium">You are rating a player on overall ability, not on
                            just todays performance</p>
                    </div>
                </div>
                <button @click="emit('update:modelValue', false)"
                    class="w-10 h-10 rounded-full hover:bg-white/5 flex items-center justify-center text-slate-500 transition-colors">
                    <X :size="20" />
                </button>
            </div>

            <!-- Body -->
            <div class="flex-1 overflow-y-auto p-8 custom-scrollbar">
                <div v-if="isLoading" class="flex flex-col items-center justify-center py-20">
                    <Loader2 :size="40" class="animate-spin text-primary mb-4" />
                    <p class="text-slate-500 font-bold uppercase tracking-widest text-xs">Loading Ratings...</p>
                </div>

                <div v-else class="space-y-12">
                    <div v-if="hasRated"
                        class="bg-success/10 border border-success/20 text-success rounded-2xl p-4 flex items-center gap-3 animate-in fade-in slide-in-from-top-4">
                        <CheckCircle2 :size="20" />
                        <p class="text-sm font-bold">You have already submitted ratings for this match.</p>
                    </div>

                    <div v-for="(players, teamNum) in teams" :key="teamNum" class="space-y-6">
                        <div class="flex items-center gap-4">
                            <h3 class="text-sm font-black text-slate-500 uppercase tracking-widest whitespace-nowrap">
                                Team {{ String.fromCharCode(64 + parseInt(teamNum as any)) }}</h3>
                            <div class="h-px w-full bg-white/5"></div>
                        </div>

                        <div class="grid grid-cols-1 md:grid-cols-2 gap-x-12 gap-y-8">
                            <div v-for="player in players" :key="player.id" class="flex flex-col gap-3">
                                <div class="flex items-center justify-between">
                                    <div class="flex items-center gap-3">
                                        <div
                                            class="w-8 h-8 rounded-lg bg-slate-950 flex items-center justify-center text-xs font-black text-primary border border-white/5">
                                            {{ player.fullName.charAt(0) }}
                                        </div>
                                        <span class="font-bold text-white">{{ player.id === currentPlayerId ? "You" :
                                            player.fullName }}</span>
                                    </div>
                                    <div class="text-xl font-black"
                                        :class="ratings[player.id] >= 7 ? 'text-primary' : ratings[player.id] <= 4 ? 'text-danger' : 'text-white'">
                                        {{ ratings[player.id] }}
                                    </div>
                                </div>

                                <input type="range" v-model.number="ratings[player.id]" min="1" max="10" step="1"
                                    :disabled="hasRated || player.id === currentPlayerId"
                                    class="w-full h-1.5 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-primary disabled:opacity-50 disabled:cursor-not-allowed" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Footer -->
            <div class="px-8 py-6 border-t border-white/5 flex items-center justify-between bg-white/[0.02]">
                <p class="text-xs text-slate-500 font-medium max-w-[200px]">Ratings are anonymous and contribute to
                    overall player rankings.</p>

                <button v-if="!hasRated" @click="handleSave" :disabled="isSaving"
                    class="flex items-center gap-2 bg-primary hover:bg-primary-dark disabled:opacity-50 text-white px-8 py-3 rounded-2xl font-black text-sm transition-all shadow-xl shadow-primary/20 active:scale-95">
                    <Loader2 v-if="isSaving" :size="18" class="animate-spin" />
                    <Save v-else :size="18" />
                    SUBMIT RATINGS
                </button>
                <button v-else @click="emit('update:modelValue', false)"
                    class="bg-slate-800 hover:bg-slate-700 text-white px-8 py-3 rounded-2xl font-black text-sm transition-all border border-white/5 active:scale-95">
                    CLOSE
                </button>
            </div>
        </div>
    </div>
</template>

<style scoped>
input[type=range]::-webkit-slider-thumb {
    -webkit-appearance: none;
    height: 16px;
    width: 16px;
    border-radius: 50%;
    background: white;
    box-shadow: 0 0 10px rgba(0, 0, 0, 0.5);
    cursor: pointer;
}

.custom-scrollbar::-webkit-scrollbar {
    width: 6px;
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

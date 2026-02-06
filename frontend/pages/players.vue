<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { Users, Star, Calendar, ShieldCheck, ShieldMinus, Search, Edit2, X, Check, Activity } from 'lucide-vue-next'

definePageMeta({
    middleware: ['auth', 'league', 'member']
})

const store = useMatchStore()
const { currentLeague } = useLeague()
const { data } = useAuth()

const isAdmin = computed(() => {
    // Check for Super Admin
    if ((data.value as any)?.user?.isSuperAdmin) return true

    // Check for League Admin
    return currentLeague.value?.role === 'Admin' || currentLeague.value?.role === 'SuperAdmin'
})

const modal = useModal()
const isModalOpen = ref(false)
const searchQuery = ref('')
const editingPlayerId = ref<string | null>(null)
const editingRating = ref(0)

const filteredPlayers = computed(() => {
    if (!searchQuery.value.trim()) return store.availablePlayers

    const query = searchQuery.value.toLowerCase()
    return store.availablePlayers.filter(p =>
        p.fullName.toLowerCase().includes(query)
    )
})

watch(currentLeague, async (newLeague) => {
    if (newLeague) {
        await store.fetchPlayers()
    }
}, { immediate: true })

const handlePromote = async (player: any) => {
    const confirmed = await modal.showConfirm(
        `Are you sure you want to promote ${player.fullName} to Admin?`,
        'Promote to Admin'
    )
    if (!confirmed) return

    const result = await store.promoteToAdmin(player.id)
    if (result.success) {
        modal.showSuccess(`${player.fullName} is now an Admin.`)
        // Refresh to get updated role
        await store.fetchPlayers()
    } else {
        modal.showError(result.error || 'Failed to promote player')
    }
}

const handleDemote = async (player: any) => {
    const confirmed = await modal.showConfirm(
        `Are you sure you want to demote ${player.fullName} to Member?`,
        'Demote to Member'
    )
    if (!confirmed) return

    const result = await store.demoteToMember(player.id)
    if (result.success) {
        modal.showSuccess(`${player.fullName} is now a Member.`)
    } else {
        modal.showError(result.error || 'Failed to demote player')
    }
}

const startEditRating = (player: any) => {
    editingPlayerId.value = player.id
    editingRating.value = player.currentRating
}

const cancelEditRating = () => {
    editingPlayerId.value = null
    editingRating.value = 0
}

const saveRating = async (player: any) => {
    if (editingRating.value < 1 || editingRating.value > 10) {
        modal.showError('Rating must be between 1 and 10')
        return
    }

    const result = await store.updatePlayerRating(player.id, editingRating.value)
    if (result.success) {
        editingPlayerId.value = null
    } else {
        alert(result.error)
    }
}

const formatDate = (dateString?: string) => {
    if (!dateString) return 'Never'
    return new Date(dateString).toLocaleDateString()
}
</script>

<template>
    <div class="max-w-7xl mx-auto px-4 py-8 md:py-12 space-y-8">
        <div class="flex flex-col md:flex-row md:items-end justify-between gap-6">
            <div class="space-y-2">
                <div class="flex items-center gap-2 text-primary font-bold text-sm uppercase tracking-widest">
                    <Users :size="16" />
                    Squad Management
                </div>
                <h1 class="text-4xl md:text-5xl font-black text-white tracking-tight">PLAYER <span
                        class="text-slate-500">LIST</span></h1>
                <p class="text-slate-400 max-w-md">Overview of all registered players and their current performance
                    ratings.
                </p>
            </div>

            <button v-if="isAdmin" @click="isModalOpen = true"
                class="bg-primary hover:bg-primary/90 text-white px-6 py-3 rounded-xl font-bold flex items-center gap-2 transition-all shadow-lg shadow-primary/20">
                <Users :size="18" />
                ADD NEW PLAYER
            </button>
        </div>

        <PlayerCreateModal :is-open="isModalOpen" @close="isModalOpen = false" />

        <!-- Search Bar -->
        <div class="relative group max-w-md">
            <Search :size="18"
                class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-primary transition-colors" />
            <input v-model="searchQuery" type="text" placeholder="Search players by name..."
                class="w-full bg-slate-900/50 border border-white/5 rounded-xl py-3 pl-12 pr-4 text-sm font-medium text-white placeholder:text-slate-500 focus:outline-none focus:border-primary/50 transition-all" />
        </div>

        <!-- No Results -->
        <div v-if="filteredPlayers.length === 0" class="text-center py-12">
            <p class="text-slate-500 text-sm">No players found matching "{{ searchQuery }}"</p>
        </div>

        <!-- Player Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div v-for="player in filteredPlayers" :key="player.id" class="glass-card p-6 flex flex-col gap-4">
                <div class="flex justify-between items-start">
                    <div>
                        <h3 class="text-xl font-bold text-white">{{ player.fullName }}</h3>
                        <p class="text-slate-500 text-sm flex items-center gap-1">
                            <Calendar :size="14" />
                            Last played: {{ formatDate(player.lastPlayed) }}
                        </p>
                    </div>

                    <!-- Rating Display/Edit -->
                    <div v-if="editingPlayerId === player.id" class="flex items-center gap-2">
                        <input v-model.number="editingRating" type="number" min="1" max="10" step="0.1"
                            class="w-16 bg-slate-900 border border-primary/50 rounded-lg px-2 py-1 text-center text-white font-black focus:outline-none focus:border-primary" />
                        <button @click="saveRating(player)"
                            class="text-success hover:text-success/80 transition-colors">
                            <Check :size="18" />
                        </button>
                        <button @click="cancelEditRating" class="text-danger hover:text-danger/80 transition-colors">
                            <X :size="18" />
                        </button>
                    </div>
                    <div v-else class="flex items-center gap-3">
                        <!-- Avg Match Rating -->
                        <div v-if="player.avgMatchRating" class="flex flex-col items-end">
                            <span class="text-[10px] uppercase font-bold text-slate-500 tracking-wider">Avg Match</span>
                            <div class="text-emerald-400 font-black flex items-center gap-1">
                                <Activity :size="14" />
                                {{ Number(player.avgMatchRating).toFixed(1) }}
                            </div>
                        </div>

                        <!-- Ability Rating -->
                        <div class="flex flex-col items-end">
                            <span class="text-[10px] uppercase font-bold text-slate-500 tracking-wider">Ability</span>
                            <div
                                class="bg-primary/10 text-primary px-3 py-1 rounded-lg font-black flex items-center gap-1">
                                <Star :size="14" class="fill-primary" />
                                {{ player.currentRating }}
                            </div>
                        </div>

                        <button v-if="isAdmin" @click="startEditRating(player)"
                            class="text-slate-500 hover:text-primary transition-colors pb-1">
                            <Edit2 :size="16" />
                        </button>
                    </div>
                </div>

                <div v-if="isAdmin && player.role !== 'Admin' && player.role !== 'SuperAdmin'"
                    class="pt-2 border-t border-white/5 mt-auto">
                    <button @click="handlePromote(player)"
                        class="w-full py-2 rounded-lg bg-white/5 hover:bg-white/10 text-slate-400 hover:text-white text-xs font-bold transition-all flex items-center justify-center gap-2 group">
                        <ShieldCheck :size="14" class="group-hover:text-primary transition-colors" />
                        PROMOTE TO ADMIN
                    </button>
                </div>

                <!-- Demote button - only for admins who are not the current user -->
                <div v-if="isAdmin && (player.role === 'Admin' || player.role === 'SuperAdmin') && player.id !== (data as any)?.user?.playerId"
                    class="pt-2 border-t border-white/5 mt-auto">
                    <button @click="handleDemote(player)"
                        class="w-full py-2 rounded-lg bg-white/5 hover:bg-white/10 text-slate-400 hover:text-red-400 text-xs font-bold transition-all flex items-center justify-center gap-2 group">
                        <ShieldMinus :size="14" class="group-hover:text-red-500 transition-colors" />
                        DEMOTE TO MEMBER
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

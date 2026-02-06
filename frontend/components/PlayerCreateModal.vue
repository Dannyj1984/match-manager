<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { X, UserPlus, Mail, Lock, Star, Loader2 } from 'lucide-vue-next'

const props = defineProps<{
    isOpen: boolean
}>()

const emit = defineEmits(['close'])

const store = useMatchStore()
const isLoading = ref(false)
const error = ref('')

const form = reactive({
    email: '',
    fullName: '',
    initialPassword: '',
    initialRating: 5.0,
    preferredPosition: 'M'
})

const resetForm = () => {
    form.email = ''
    form.fullName = ''
    form.initialPassword = ''
    form.initialRating = 5.0,
        form.preferredPosition = 'M'
    error.value = ''
}


const { currentLeague } = useLeague()
const leaguePositions = currentLeague.value?.sport === "Netball" ? ['GK', 'GD', 'WD', 'C', 'WA', 'GA', 'GS'] : ['GK', 'D', 'M', 'A']
const handleSubmit = async () => {
    isLoading.value = true
    error.value = ''

    if (!currentLeague.value?.id) {
        error.value = 'No active league selected'
        isLoading.value = false
        return
    }

    const result = await store.createPlayer({
        ...form,
        leagueId: currentLeague.value.id
    })

    if (result.success) {
        resetForm()
        emit('close')
    } else {
        error.value = result.error
    }

    isLoading.value = false
}
</script>

<template>
    <div v-if="isOpen" class="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6">
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-background/80 backdrop-blur-sm" @click="emit('close')"></div>

        <!-- Modal Content -->
        <div
            class="relative w-full max-w-lg glass-card p-6 md:p-8 space-y-6 shadow-2xl animate-in fade-in zoom-in duration-200">
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
                        <UserPlus :size="20" />
                    </div>
                    <div>
                        <h2 class="text-xl font-bold text-white">Add New Player</h2>
                        <p class="text-sm text-slate-400">Create a user account and player profile.</p>
                    </div>
                </div>
                <button @click="emit('close')"
                    class="p-2 hover:bg-white/5 rounded-lg transition-colors text-slate-500 hover:text-white">
                    <X :size="20" />
                </button>
            </div>

            <form @submit.prevent="handleSubmit" class="space-y-4">
                <div v-if="error"
                    class="bg-danger/10 border border-danger/20 text-danger text-sm p-3 rounded-lg flex items-center gap-2">
                    <span class="w-1.5 h-1.5 rounded-full bg-danger animate-pulse"></span>
                    {{ error }}
                </div>

                <div class="space-y-4">
                    <div class="space-y-1.5">
                        <label
                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                            <Mail :size="12" /> Email Address
                        </label>
                        <input v-model="form.email" type="email" required placeholder="player@example.com"
                            class="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 transition-colors" />
                    </div>

                    <div class="space-y-1.5">
                        <label
                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                            <UserPlus :size="12" /> Full Name
                        </label>
                        <input v-model="form.fullName" type="text" required placeholder="John Doe"
                            class="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 transition-colors" />
                    </div>

                    <div class="space-y-1.5">
                        <label
                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                            <Lock :size="12" /> Initial Password
                        </label>
                        <input v-model="form.initialPassword" type="password" required placeholder="••••••••"
                            class="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 transition-colors" />
                    </div>

                    <div class="space-y-1.5">
                        <label
                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                            <Star :size="12" /> Preferred Position
                        </label>
                        <div class="grid grid-cols-4 gap-2">
                            <button v-for="pos in leaguePositions" :key="pos" type="button"
                                @click="form.preferredPosition = pos" :class="[
                                    'py-2.5 rounded-xl border text-xs font-black transition-all',
                                    form.preferredPosition === pos
                                        ? 'bg-primary/20 border-primary text-primary shadow-lg shadow-primary/10'
                                        : 'bg-white/5 border-white/5 text-slate-500 hover:border-white/10 hover:text-slate-300'
                                ]">
                                {{ pos }}
                            </button>
                        </div>
                    </div>

                    <div class="space-y-1.5">
                        <label
                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                            <Star :size="12" /> Initial Rating (1-10)
                        </label>
                        <input v-model="form.initialRating" type="number" step="0.1" min="1" max="10" required
                            class="w-full bg-white/5 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 transition-colors" />
                    </div>
                </div>

                <div class="pt-4">
                    <button type="submit" :disabled="isLoading"
                        class="w-full bg-primary hover:bg-primary-dark text-white font-bold py-4 rounded-xl shadow-lg shadow-primary/20 transition-all flex items-center justify-center gap-2 group disabled:opacity-50 disabled:cursor-not-allowed">
                        <Loader2 v-if="isLoading" :size="20" class="animate-spin" />
                        <span v-else>CREATE PLAYER PROFILE</span>
                    </button>
                </div>
            </form>
        </div>
    </div>
</template>

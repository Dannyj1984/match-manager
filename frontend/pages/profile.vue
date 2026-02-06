<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { User, Mail, Star, Save, Loader2, CheckCircle } from 'lucide-vue-next'

const { currentLeague } = useLeague()
const { getPositionsForSport } = useSportPositions()

const store = useMatchStore()
const isLoading = ref(false)
const isSaving = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const form = reactive({
    email: '',
    fullName: '',
    preferredPosition: 'M',
    currentRating: 0
})

// Get positions for current league's sport
const availablePositions = computed(() => {
    return getPositionsForSport(currentLeague.value?.sport || 'Football')
})

onMounted(async () => {
    isLoading.value = true
    const profile = await store.fetchMe()
    if (profile) {
        form.email = profile.email
        form.fullName = profile.fullName
        form.preferredPosition = profile.preferredPosition
        form.currentRating = profile.currentRating
    }
    isLoading.value = false
})

const handleSave = async () => {
    isSaving.value = true
    successMessage.value = ''
    errorMessage.value = ''

    const result = await store.updateProfile({
        fullName: form.fullName,
        preferredPosition: form.preferredPosition
    })

    if (result.success) {
        successMessage.value = 'Profile updated successfully!'
        setTimeout(() => successMessage.value = '', 3000)
    } else {
        errorMessage.value = result.error
    }
    isSaving.value = false
}

definePageMeta({
    middleware: ['auth', 'league', 'member']
})
</script>

<template>
    <div class="max-w-4xl mx-auto px-4 py-12 md:py-20">
        <div class="space-y-8">
            <!-- Header -->
            <div class="space-y-2">
                <div class="flex items-center gap-2 text-primary font-bold text-sm uppercase tracking-widest">
                    <User :size="16" />
                    Account Settings
                </div>
                <h1 class="text-4xl md:text-5xl font-black text-white tracking-tight">YOUR <span
                        class="text-slate-500">PROFILE</span></h1>
                <p class="text-slate-400">Manage your personal information and field preferences.</p>
            </div>

            <div v-if="isLoading" class="flex items-center justify-center py-20">
                <Loader2 :size="40" class="animate-spin text-primary" />
            </div>

            <div v-else class="grid grid-cols-1 md:grid-cols-3 gap-8">
                <!-- Left: Stats Card -->
                <div class="md:col-span-1 space-y-6">
                    <div
                        class="glass-card p-8 flex flex-col items-center text-center border-white/5 relative overflow-hidden">
                        <div class="absolute top-0 right-0 p-4">
                            <div
                                class="bg-primary/20 text-primary text-[10px] font-black px-2 py-1 rounded-full border border-primary/20">
                                ACTIVE PLAYER
                            </div>
                        </div>

                        <div
                            class="w-24 h-24 rounded-3xl bg-slate-900 flex items-center justify-center text-4xl font-black text-primary border border-white/5 mb-6 shadow-2xl shadow-primary/10">
                            {{ form.fullName.charAt(0).toUpperCase() }}
                        </div>

                        <h2 class="text-xl font-bold text-white leading-tight mb-1">{{ form.fullName }}</h2>
                        <p class="text-sm text-slate-500 font-medium mb-6">{{ form.email }}</p>

                        <div class="w-full grid grid-cols-2 gap-4">
                            <div class="bg-slate-900/50 p-4 rounded-2xl border border-white/5">
                                <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1">Rating
                                </p>
                                <p class="text-xl font-black text-white">{{ form.currentRating.toFixed(1) }}</p>
                            </div>
                            <div class="bg-slate-900/50 p-4 rounded-2xl border border-white/5">
                                <p class="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1">Pos</p>
                                <p class="text-xl font-black text-primary">{{ form.preferredPosition }}</p>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Right: Form -->
                <div class="md:col-span-2">
                    <div class="glass-card p-8 border-white/5">
                        <form @submit.prevent="handleSave" class="space-y-6">
                            <div v-if="successMessage"
                                class="bg-success/10 border border-success/20 text-success text-sm p-4 rounded-xl flex items-center gap-3 animate-in fade-in slide-in-from-top-2">
                                <CheckCircle :size="18" />
                                {{ successMessage }}
                            </div>

                            <div v-if="errorMessage"
                                class="bg-danger/10 border border-danger/20 text-danger text-sm p-4 rounded-xl flex items-center gap-3 animate-in fade-in slide-in-from-top-2">
                                <div class="w-1.5 h-1.5 rounded-full bg-danger animate-pulse"></div>
                                {{ errorMessage }}
                            </div>

                            <div class="space-y-6">
                                <!-- Email (Read Only) -->
                                <div class="space-y-2">
                                    <label
                                        class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                                        <Mail :size="12" /> Email Address
                                    </label>
                                    <div
                                        class="w-full bg-white/5 border border-white/5 rounded-xl px-4 py-4 text-slate-400 font-medium cursor-not-allowed">
                                        {{ form.email }}
                                    </div>
                                    <p class="text-[10px] text-slate-600 italic">Email cannot be changed. Contact admin
                                        for updates.</p>
                                </div>

                                <!-- Full Name -->
                                <div class="space-y-2">
                                    <label
                                        class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                                        <User :size="12" /> Full Name
                                    </label>
                                    <input v-model="form.fullName" type="text" required
                                        class="w-full bg-slate-900/50 border border-white/10 rounded-xl px-4 py-4 text-white focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/20 transition-all" />
                                </div>

                                <!-- Position Selector -->
                                <div class="space-y-3">
                                    <label
                                        class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                                        <Star :size="12" /> Preferred Position
                                    </label>
                                    <div class="grid grid-cols-4 gap-4">
                                        <button v-for="pos in availablePositions" :key="pos" type="button"
                                            @click="form.preferredPosition = pos" :class="[
                                                'py-4 rounded-2xl border text-sm font-black transition-all group relative overflow-hidden',
                                                form.preferredPosition === pos
                                                    ? 'bg-primary border-primary text-white shadow-xl shadow-primary/20'
                                                    : 'bg-slate-900/50 border-white/5 text-slate-400 hover:border-white/20 hover:text-white'
                                            ]">
                                            {{ pos }}
                                            <div v-if="form.preferredPosition === pos"
                                                class="absolute inset-0 bg-white/10 animate-pulse"></div>
                                        </button>
                                    </div>
                                </div>
                            </div>

                            <div class="pt-6 border-t border-white/5">
                                <button type="submit" :disabled="isSaving"
                                    class="w-full md:w-auto min-w-[200px] bg-primary hover:bg-primary-dark text-white font-black py-4 px-8 rounded-2xl shadow-xl shadow-primary/20 transition-all flex items-center justify-center gap-3 active:scale-95 disabled:opacity-50">
                                    <Loader2 v-if="isSaving" :size="20" class="animate-spin" />
                                    <Save v-else :size="20" />
                                    SAVE SETTINGS
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

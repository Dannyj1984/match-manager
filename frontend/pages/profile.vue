<script setup lang="ts">
import { useMatchStore } from '@/stores/match'
import { User, Mail, Star, Save, Loader2, CheckCircle, Lock, ChevronDown, Trophy, Flame, Clock, Medal } from 'lucide-vue-next'
import type { ApiError } from '~/types/api'
import type { PlayerStats } from '~/types/player'

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
    preferredPosition: ['M'],
    currentRating: 0
})

const stats = ref({
    gamesPlayed: 0,
    currentStreak: 0,
    highestRating4Weeks: null as number | null,
    isGoldenBoot: false,
    earlyBirdCount: 0
})

const passwordForm = reactive({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
})

const isChangingPassword = ref(false)
const passwordSuccessMessage = ref('')
const passwordErrorMessage = ref('')
const isPasswordSectionOpen = ref(false)

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

        // Fetch Stats
        try {
            const { token } = useAuth()
            const statsData = await $fetch<PlayerStats>(`/api/players/${profile.id}/stats`, {
                headers: {
                    'Authorization': token.value || '',
                    'X-League-Id': currentLeague.value?.id || ''
                }
            })
            stats.value = statsData
        } catch (e) {
            console.error('Failed to fetch stats', e)
        }
    }
    isLoading.value = false
})

const filterPositions = (sport: string, positions: string[]): string[] => {
    const allowed = sport.toLowerCase() === 'netball' ? ['GK', 'GD', 'WD', 'C', 'WA', 'GA', 'GS'] : ['GK', 'D', 'M', 'A']
    return positions.filter((p) => allowed.includes(p))
}

const handleSave = async () => {
    isSaving.value = true
    successMessage.value = ''
    errorMessage.value = ''

    const result = await store.updateProfile({
        fullName: form.fullName,
        preferredPosition: filterPositions(currentLeague.value?.sport || 'Football', form.preferredPosition)
    })

    if (result.success) {
        successMessage.value = 'Profile updated successfully!'
        setTimeout(() => successMessage.value = '', 3000)
    } else {
        errorMessage.value = result.error || 'Failed to update profile'
    }
    isSaving.value = false
}

const togglePosition = (pos: string) => {
    const index = form.preferredPosition.indexOf(pos)
    if (index > -1) {
        // Remove if already selected (but keep at least one)
        if (form.preferredPosition.length > 1) {
            form.preferredPosition.splice(index, 1)
        }
    } else {
        form.preferredPosition.push(pos)
    }

}

const handlePasswordChange = async () => {
    isChangingPassword.value = true
    passwordSuccessMessage.value = ''
    passwordErrorMessage.value = ''

    // Validate passwords match
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
        passwordErrorMessage.value = 'New passwords do not match'
        isChangingPassword.value = false
        return
    }

    // Validate password length
    if (passwordForm.newPassword.length < 6) {
        passwordErrorMessage.value = 'Password must be at least 6 characters'
        isChangingPassword.value = false
        return
    }

    try {
        const { token } = useAuth()
        if (!token.value) {
            passwordErrorMessage.value = 'Not authenticated'
            isChangingPassword.value = false
            return
        }

        await $fetch('/api/auth/change-password', {
            method: 'POST',
            headers: {
                'Authorization': token.value,
                'Content-Type': 'application/json'
            },
            body: {
                currentPassword: passwordForm.currentPassword,
                newPassword: passwordForm.newPassword
            }
        })

        passwordSuccessMessage.value = 'Password changed successfully!'
        // Reset form
        passwordForm.currentPassword = ''
        passwordForm.newPassword = ''
        passwordForm.confirmPassword = ''
        setTimeout(() => passwordSuccessMessage.value = '', 3000)
    } catch (error: unknown) {
        const apiErr = error as ApiError
        passwordErrorMessage.value = apiErr.data?.message || 'Failed to change password'
    }

    isChangingPassword.value = false
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
                                <p class="text-xl font-black text-primary">{{ form.preferredPosition.join(', ') }}</p>
                            </div>
                        </div>
                    </div>

                    <!-- Achievements Card -->
                    <div class="glass-card p-6 border-white/5 space-y-4">
                        <h3 class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-2">
                            <Trophy :size="14" /> Stats & Badges
                        </h3>

                        <div class="grid grid-cols-2 gap-3">
                            <!-- Games Played -->
                            <div
                                class="bg-slate-900/50 p-3 rounded-xl border border-white/5 flex flex-col items-center text-center group hover:border-blue-500/30 transition-colors">
                                <div
                                    class="w-8 h-8 rounded-full bg-blue-500/20 text-blue-500 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
                                    <Medal :size="16" />
                                </div>
                                <span class="text-2xl font-black text-white">{{ stats.gamesPlayed }}</span>
                                <span class="text-[10px] text-slate-500 uppercase font-bold">Games</span>
                            </div>

                            <!-- Streak -->
                            <div
                                class="bg-slate-900/50 p-3 rounded-xl border border-white/5 flex flex-col items-center text-center group hover:border-orange-500/30 transition-colors">
                                <div
                                    class="w-8 h-8 rounded-full bg-orange-500/20 text-orange-500 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
                                    <Flame :size="16" />
                                </div>
                                <span class="text-2xl font-black text-white">{{ stats.currentStreak }}</span>
                                <span class="text-[10px] text-slate-500 uppercase font-bold">Streak</span>
                            </div>

                            <!-- Golden Boot / 4 Week Rating -->
                            <div class="bg-slate-900/50 p-3 rounded-xl border border-white/5 flex flex-col items-center text-center relative overflow-hidden group hover:border-yellow-500/30 transition-colors"
                                :class="{ 'ring-1 ring-yellow-500/50': stats.isGoldenBoot }">
                                <div class="absolute inset-0 bg-yellow-500/10" v-if="stats.isGoldenBoot"></div>
                                <div
                                    class="w-8 h-8 rounded-full bg-yellow-500/20 text-yellow-500 flex items-center justify-center mb-2 relative z-10 group-hover:scale-110 transition-transform">
                                    <Trophy :size="16" />
                                </div>
                                <span class="text-lg font-black text-white relative z-10">{{ stats.highestRating4Weeks ?
                                    stats.highestRating4Weeks.toFixed(1) : '-' }}</span>
                                <span class="text-[10px] text-slate-500 uppercase font-bold relative z-10">4-Wk
                                    Rating</span>
                                <div v-if="stats.isGoldenBoot"
                                    class="absolute top-0 right-0 w-3 h-3 bg-yellow-500 rounded-bl-lg"></div>
                            </div>

                            <!-- Early Bird -->
                            <div
                                class="bg-slate-900/50 p-3 rounded-xl border border-white/5 flex flex-col items-center text-center group hover:border-emerald-500/30 transition-colors">
                                <div
                                    class="w-8 h-8 rounded-full bg-emerald-500/20 text-emerald-500 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
                                    <Clock :size="16" />
                                </div>
                                <span class="text-2xl font-black text-white">{{ stats.earlyBirdCount }}</span>
                                <span class="text-[10px] text-slate-500 uppercase font-bold">Early Bird</span>
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

                                <!-- Password Change Section -->
                                <div class="pt-4 border-t border-white/5">
                                    <!-- Accordion Header -->
                                    <button type="button" @click="isPasswordSectionOpen = !isPasswordSectionOpen"
                                        class="w-full flex items-center justify-between py-3 px-4 rounded-xl hover:bg-white/5 transition-colors group">
                                        <label
                                            class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5 cursor-pointer">
                                            <Lock :size="12" /> Change Password
                                        </label>
                                        <ChevronDown :size="16" :class="[
                                            'text-slate-500 transition-transform duration-200',
                                            isPasswordSectionOpen ? 'rotate-180' : ''
                                        ]" />
                                    </button>

                                    <!-- Accordion Content -->
                                    <div v-show="isPasswordSectionOpen"
                                        class="space-y-4 pt-4 animate-in fade-in slide-in-from-top-2 duration-200">
                                        <div v-if="passwordSuccessMessage"
                                            class="bg-success/10 border border-success/20 text-success text-sm p-3 rounded-xl flex items-center gap-3 animate-in fade-in slide-in-from-top-2">
                                            <CheckCircle :size="18" />
                                            {{ passwordSuccessMessage }}
                                        </div>

                                        <div v-if="passwordErrorMessage"
                                            class="bg-danger/10 border border-danger/20 text-danger text-sm p-3 rounded-xl flex items-center gap-3 animate-in fade-in slide-in-from-top-2">
                                            <div class="w-1.5 h-1.5 rounded-full bg-danger animate-pulse"></div>
                                            {{ passwordErrorMessage }}
                                        </div>

                                        <div class="space-y-3">
                                            <div class="space-y-2">
                                                <label
                                                    class="text-xs font-bold text-slate-400 uppercase tracking-wider">
                                                    Current Password
                                                </label>
                                                <input v-model="passwordForm.currentPassword" type="password"
                                                    placeholder="Enter current password"
                                                    class="w-full bg-slate-900/50 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/20 transition-all" />
                                            </div>

                                            <div class="space-y-2">
                                                <label
                                                    class="text-xs font-bold text-slate-400 uppercase tracking-wider">
                                                    New Password
                                                </label>
                                                <input v-model="passwordForm.newPassword" type="password"
                                                    placeholder="Enter new password"
                                                    class="w-full bg-slate-900/50 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/20 transition-all" />
                                            </div>

                                            <div class="space-y-2">
                                                <label
                                                    class="text-xs font-bold text-slate-400 uppercase tracking-wider">
                                                    Confirm New Password
                                                </label>
                                                <input v-model="passwordForm.confirmPassword" type="password"
                                                    placeholder="Confirm new password"
                                                    class="w-full bg-slate-900/50 border border-white/10 rounded-xl px-4 py-3 text-white placeholder:text-slate-600 focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/20 transition-all" />
                                            </div>

                                            <button type="button" @click="handlePasswordChange"
                                                :disabled="isChangingPassword"
                                                class="w-full bg-slate-800 hover:bg-slate-700 text-white font-bold py-3 px-6 rounded-xl border border-white/10 transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
                                                <Loader2 v-if="isChangingPassword" :size="18" class="animate-spin" />
                                                <Lock v-else :size="18" />
                                                <span>{{ isChangingPassword ? 'Updating...' : 'Update Password'
                                                    }}</span>
                                            </button>
                                        </div>
                                    </div>
                                </div>

                                <!-- Position Selector -->
                                <div class="space-y-3">
                                    <label
                                        class="text-xs font-bold text-slate-500 uppercase tracking-widest flex items-center gap-1.5">
                                        <Star :size="12" /> Preferred Position
                                    </label>
                                    <div class="grid grid-cols-4 gap-4">
                                        <button v-for="pos in availablePositions" :key="pos" type="button"
                                            @click="togglePosition(pos)" :class="[
                                                'py-4 rounded-2xl border text-sm font-black transition-all group relative overflow-hidden',
                                                form.preferredPosition.includes(pos)
                                                    ? 'bg-primary border-primary text-white shadow-xl shadow-primary/20'
                                                    : 'bg-slate-900/50 border-white/5 text-slate-400 hover:border-white/20 hover:text-white'
                                            ]">
                                            {{ pos }}
                                            <div v-if="form.preferredPosition.includes(pos)"
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

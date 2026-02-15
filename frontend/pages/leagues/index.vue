<template>
  <div class="container mx-auto px-6 py-12">
    <div class="max-w-4xl mx-auto">
      <div class="mb-8 flex items-center justify-between">
        <h1 class="text-3xl font-bold">My Leagues</h1>
        <NuxtLink to="/leagues/search"
          class="px-4 py-2 bg-slate-700/60 hover:bg-slate-600/60 text-white font-medium rounded-lg transition-colors flex items-center gap-2">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"
            stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          Find Leagues
        </NuxtLink>
      </div>

      <div v-if="userLeagues.length > 0" class="grid gap-4">
        <NuxtLink v-for="league in userLeagues" :key="league.id" :to="`/leagues/${league.id}`"
          class="glass-card p-6 hover:border-primary/30 transition-colors cursor-pointer block">
          <div class="flex justify-between items-start">
            <div>
              <h2 class="text-xl font-bold mb-1">{{ league.name }}</h2>
              <div class="flex items-center gap-3 text-sm text-slate-400">
                <span>{{ league.sport }}</span>
                <span v-if="league.location">· {{ league.location }}</span>
                <span v-if="league.postcode">· {{ league.postcode }}</span>
                <span v-if="league.isPublic"
                  class="px-2 py-0.5 text-xs bg-blue-500/20 text-blue-400 rounded">Public</span>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span v-if="league.pendingJoinRequests > 0"
                class="px-2 py-1 text-xs font-bold bg-amber-500/20 text-amber-400 rounded-full">
                {{ league.pendingJoinRequests }} pending
              </span>
              <span class="px-3 py-1 text-xs font-medium rounded"
                :class="league.role === 'Admin' || league.role === 'SuperAdmin' ? 'bg-green-500/20 text-green-400' : 'bg-slate-700 text-slate-400'">
                {{ league.role }}
              </span>
            </div>
          </div>
        </NuxtLink>
      </div>

      <div v-else class="glass-card p-12 text-center">
        <p class="text-slate-400 mb-4">You're not a member of any leagues yet.</p>
        <NuxtLink to="/leagues/search" class="btn-primary inline-block">Find Leagues</NuxtLink>
      </div>

      <!-- Create League (Super Admin Only) -->
      <div v-if="(data as unknown as AuthSessionData | null)?.user?.isSuperAdmin" class="mt-12">
        <h2 class="text-2xl font-bold mb-4">Create New League</h2>
        <div class="glass-card p-6">
          <form @submit.prevent="handleCreateLeague" class="space-y-4">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium mb-2">League Name</label>
                <input v-model="createForm.name" type="text" required
                  class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
              </div>
              <div>
                <label class="block text-sm font-medium mb-2">Sport</label>
                <select v-model="createForm.sport" required
                  class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
                  <option value="Football">Football</option>
                  <option value="Netball">Netball</option>
                  <option value="Basketball">Basketball</option>
                  <option value="Rugby">Rugby</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium mb-2">Max Teams</label>
                <input v-model.number="createForm.maxTeams" type="number" min="2" max="10" required
                  class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
              </div>
              <div>
                <label class="block text-sm font-medium mb-2">Location</label>
                <input v-model="createForm.location" type="text"
                  class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
                  placeholder="e.g. Central Park Sports Centre" />
              </div>
              <div>
                <label class="block text-sm font-medium mb-2">Postcode</label>
                <input v-model="createForm.postcode" type="text"
                  class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
                  placeholder="e.g. M1 3AA" />
                <p class="text-xs text-slate-500 mt-1">UK postcodes only. Used for league search.</p>
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium mb-2">Description</label>
              <textarea v-model="createForm.description" rows="3"
                class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"></textarea>
            </div>
            <div class="flex items-center gap-3">
              <input v-model="createForm.isPublic" type="checkbox" id="createIsPublic"
                class="w-5 h-5 rounded border-gray-300 text-primary focus:ring-primary" />
              <label for="createIsPublic" class="text-sm font-medium">Public League</label>
              <span class="text-xs text-slate-500">(Discoverable via search)</span>
            </div>
            <button type="submit" class="btn-primary">Create League</button>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { AuthSessionData } from '~/types/auth'

definePageMeta({
  middleware: 'auth'
})

const { data } = useAuth()
const { userLeagues, fetchUserLeagues, createLeague } = useLeague()
const modal = useModal()

const createForm = ref({
  name: '',
  sport: 'Football',
  maxTeams: 2,
  location: '',
  description: '',
  isPublic: false,
  postcode: ''
})

onMounted(async () => {
  await fetchUserLeagues()
})

const handleCreateLeague = async () => {
  try {
    const userId = (data.value as AuthSessionData | null)?.user?.playerId
    const result = await createLeague({
      ...createForm.value,
      initialAdminUserId: userId ?? undefined
    })

    modal.showInfo('League created successfully!', 'Success')

    createForm.value = {
      name: '',
      sport: 'Football',
      maxTeams: 2,
      location: '',
      description: '',
      isPublic: false,
      postcode: ''
    }
  } catch (error) {
    console.error('Failed to create league:', error)
    modal.showError('Failed to create league', 'Error')
  }
}
</script>

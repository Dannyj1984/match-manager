<template>
  <div class="container mx-auto px-6 py-12">
    <div class="max-w-4xl mx-auto">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-bold mb-2">Leagues</h1>
          <p class="text-slate-400">Manage your leagues and memberships</p>
        </div>
        <button v-if="isSuperAdmin" @click="showCreateForm = true" class="btn-primary">
          + Create League
        </button>
      </div>

      <!-- Create League Form -->
      <div v-if="showCreateForm" class="glass-card p-6 mb-8">
        <h2 class="text-xl font-bold mb-4">Create New League</h2>
        <form @submit.prevent="handleCreateLeague" class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-2">League Name</label>
            <input v-model="newLeague.name" type="text" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="Monday Night Football" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Sport</label>
            <select v-model="newLeague.sport" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
              <option value="Football">Football</option>
              <option value="Netball">Netball</option>
              <option value="Basketball">Basketball</option>
              <option value="Rugby">Rugby</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Max Teams</label>
            <input v-model.number="newLeague.maxTeams" type="number" min="2" max="10" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Default Location</label>
            <input v-model="newLeague.location" type="text"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="Central Park" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Description</label>
            <textarea v-model="newLeague.description" rows="3"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="Casual weekly games..."></textarea>
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Cost per Match</label>
            <input v-model.number="newLeague.cost" type="number" step="0.01" min="0" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="5.00" />
          </div>

          <div class="flex gap-3">
            <button type="submit" class="btn-primary">Create League</button>
            <button type="button" @click="showCreateForm = false" class="btn-secondary">Cancel</button>
          </div>
        </form>
      </div>

      <!-- Leagues List -->
      <div class="space-y-4">
        <div v-for="league in userLeagues" :key="league.Id" class="glass-card p-6">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center gap-3 mb-2">
                <h3 class="text-xl font-bold">{{ league.name }}</h3>
                <span class="px-2 py-1 text-xs font-medium bg-primary/20 text-primary rounded">
                  {{ league.sport }}
                </span>
                <span v-if="league.role === 'Admin' || league.role === 'SuperAdmin'"
                  class="px-2 py-1 text-xs font-medium bg-green-500/20 text-green-400 rounded">
                  {{ league.role }}
                </span>
              </div>
              <p v-if="league.description" class="text-slate-400 text-sm mb-3">{{ league.description }}</p>
              <div class="flex gap-4 text-sm text-slate-500">
                <span>Max Teams: {{ league.maxTeams }}</span>
                <span v-if="league.location">📍 {{ league.location }}</span>
                <span v-if="league.cost">💰 £{{ league.cost.toFixed(2) }}</span>
              </div>
            </div>
            <div class="flex gap-2">
              <NuxtLink v-if="league.role === 'Admin' || league.role === 'SuperAdmin'" :to="`/leagues/${league.id}`"
                class="btn-secondary text-sm">
                Manage
              </NuxtLink>
              <span v-else class="px-3 py-2 text-sm text-green-400 font-medium">Active</span>
            </div>
          </div>
        </div>

        <div v-if="userLeagues.length === 0" class="glass-card p-12 text-center">
          <p class="text-slate-400 mb-4">You're not a member of any leagues yet.</p>
          <button v-if="isSuperAdmin" @click="showCreateForm = true" class="btn-primary">
            Create Your First League
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: 'auth'
})

const { currentLeague, userLeagues, fetchUserLeagues, createLeague, setCurrentLeague } = useLeague()
const { data } = useAuth()
const router = useRouter()


const showCreateForm = ref(false)
const newLeague = ref({
  name: '',
  sport: 'Football',
  maxTeams: 2,
  location: '',
  description: '',
  cost: 0
})

// Check if user is super admin  
const isSuperAdmin = computed(() => (data.value as any)?.user?.isSuperAdmin === true)

onMounted(async () => {
  await fetchUserLeagues()
})

const handleCreateLeague = async () => {
  try {
    await createLeague(newLeague.value)
    showCreateForm.value = false
    newLeague.value = {
      name: '',
      sport: 'Football',
      maxTeams: 2,
      location: '',
      description: '',
      cost: 0
    }
  } catch (error) {
    console.error('Failed to create league:', error)
  }
}

const switchLeague = (league: any) => {
  setCurrentLeague(league)
  if (!isSuperAdmin.value) {
    router.push('/')
  }
}
</script>

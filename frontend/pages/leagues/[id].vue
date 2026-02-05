<template>
  <div class="container mx-auto px-6 py-12">
    <div class="max-w-4xl mx-auto">
      <div class="mb-8">
        <NuxtLink to="/leagues" class="text-sm text-slate-400 hover:text-white mb-4 inline-block">
          ← Back to Leagues
        </NuxtLink>
        <h1 class="text-3xl font-bold mb-2">Manage League</h1>
        <p class="text-slate-400">{{ league?.name }}</p>
      </div>

      <!-- League Settings -->
      <div class="glass-card p-6 mb-6">
        <h2 class="text-xl font-bold mb-4">League Settings</h2>
        <form @submit.prevent="handleUpdateLeague" class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-2">League Name</label>
            <input v-model="leagueForm.name" type="text" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Sport</label>
            <select v-model="leagueForm.sport" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
              <option value="Football">Football</option>
              <option value="Netball">Netball</option>
              <option value="Basketball">Basketball</option>
              <option value="Rugby">Rugby</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Max Teams</label>
            <input v-model.number="leagueForm.maxTeams" type="number" min="2" max="10" required
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Default Location</label>
            <input v-model="leagueForm.location" type="text"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Description</label>
            <textarea v-model="leagueForm.description" rows="3"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"></textarea>
          </div>

          <button type="submit" class="btn-primary">Save Changes</button>
        </form>
      </div>

      <!-- Create League Admin -->
      <div class="glass-card p-6 mb-6">
        <h2 class="text-xl font-bold mb-4">Create League Admin</h2>
        <p class="text-slate-400 text-sm mb-4">Create a new user account and assign them as an admin for this league.
        </p>

        <form @submit.prevent="handleCreateAdmin" class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium mb-2">Full Name</label>
              <input v-model="adminForm.fullName" type="text" required
                class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
            </div>
            <div>
              <label class="block text-sm font-medium mb-2">Email</label>
              <input v-model="adminForm.email" type="email" required
                class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
            </div>
            <div>
              <label class="block text-sm font-medium mb-2">Password</label>
              <input v-model="adminForm.password" type="password" required
                class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
            </div>
            <div>
              <label class="block text-sm font-medium mb-2">Preferred Position (Optional)</label>
              <select v-model="adminForm.preferredPosition"
                class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
                <option value="">Any</option>
                <option value="Goalkeeper">Goalkeeper</option>
                <option value="Defender">Defender</option>
                <option value="Midfielder">Midfielder</option>
                <option value="Forward">Forward</option>
              </select>
            </div>
          </div>
          <button type="submit" class="btn-primary">Create Admin Account</button>
        </form>
      </div>

      <!-- Members Management -->
      <div class="glass-card p-6">
        <h2 class="text-xl font-bold mb-4">Members</h2>

        <!-- Add Member Form -->
        <form @submit.prevent="handleAddMember" class="flex gap-3 mb-6">
          <input v-model="newMemberEmail" type="email" placeholder="Enter member email" required
            class="flex-1 px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          <button type="submit" class="btn-primary">Add Member</button>
        </form>

        <!-- Members List -->
        <div class="space-y-3">
          <div v-for="member in members" :key="member.UserId"
            class="flex items-center justify-between p-4 bg-slate-800/50 rounded-lg">
            <div>
              <div class="font-medium">{{ member.email }}</div>
              <div class="text-sm text-slate-400">Joined {{ new Date(member.joinedDate).toLocaleDateString() }}</div>
            </div>
            <div class="flex items-center gap-3">
              <span :class="member.role === 'Admin' ? 'bg-green-500/20 text-green-400' : 'bg-slate-700 text-slate-400'"
                class="px-3 py-1 text-xs font-medium rounded">
                {{ member.role }}
              </span>
              <button v-if="member.Role === 'Member'" @click="handlePromote(member.UserId)"
                class="text-sm text-blue-400 hover:text-blue-300">
                Promote
              </button>
              <button v-else-if="member.Role === 'Admin'" @click="handleDemote(member.UserId)"
                class="text-sm text-orange-400 hover:text-orange-300">
                Demote
              </button>
              <button @click="handleRemove(member.UserId)" class="text-sm text-red-400 hover:text-red-300">
                Remove
              </button>
            </div>
          </div>
          <div v-if="members.length === 0" class="text-center py-8 text-slate-400">
            No members yet
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: 'auth'
})

const route = useRoute()

const { updateLeague, getLeague, getLeagueMembers, addMember, removeMember, promoteToAdmin, demoteAdmin, createLeagueAdmin } = useLeague()

const leagueId = route.params.id as string
const league = ref<any>(null)
const members = ref<any[]>([])
const newMemberEmail = ref('')

const leagueForm = ref({
  name: '',
  sport: 'Football',
  maxTeams: 2,
  location: '',
  description: ''
})

const adminForm = ref({
  fullName: '',
  email: '',
  password: '',
  preferredPosition: ''
})

onMounted(async () => {
  // Fetch league details and members
  await loadLeagueData()
})

const loadLeagueData = async () => {
  try {
    // Fetch league details
    league.value = await getLeague(leagueId)

    // Populate form with league data
    if (league.value) {
      leagueForm.value = {
        name: league.value.name,
        sport: league.value.sport,
        maxTeams: league.value.maxTeams,
        location: league.value.location || '',
        description: league.value.description || ''
      }
    }

    // Fetch members
    members.value = await getLeagueMembers(leagueId) as any[]
  } catch (error) {
    console.error('Failed to load league data:', error)
  }
}

const handleUpdateLeague = async () => {
  try {
    await updateLeague(leagueId, leagueForm.value)
    alert('League updated successfully!')
  } catch (error) {
    console.error('Failed to update league:', error)
    alert('Failed to update league')
  }
}

const handleCreateAdmin = async () => {
  try {
    await createLeagueAdmin(leagueId, adminForm.value)
    alert('League admin created successfully!')
    // Reset form
    adminForm.value = {
      fullName: '',
      email: '',
      password: '',
      preferredPosition: ''
    }
    // Reload members list
    await loadLeagueData()
  } catch (error: any) {
    console.error('Failed to create league admin:', error)
    alert(error.data?.Message || 'Failed to create league admin')
  }
}

const handleAddMember = async () => {
  try {
    await addMember(leagueId, newMemberEmail.value)
    newMemberEmail.value = ''
    await loadLeagueData()
  } catch (error) {
    console.error('Failed to add member:', error)
    alert('Failed to add member')
  }
}

const handleRemove = async (userId: string) => {
  if (confirm('Are you sure you want to remove this member?')) {
    try {
      await removeMember(leagueId, userId)
      await loadLeagueData()
    } catch (error) {
      console.error('Failed to remove member:', error)
      alert('Failed to remove member')
    }
  }
}

const handlePromote = async (userId: string) => {
  try {
    await promoteToAdmin(leagueId, userId)
    await loadLeagueData()
  } catch (error) {
    console.error('Failed to promote user:', error)
    alert('Failed to promote user')
  }
}

const handleDemote = async (userId: string) => {
  try {
    await demoteAdmin(leagueId, userId)
    await loadLeagueData()
  } catch (error) {
    console.error('Failed to demote user:', error)
    alert('Failed to demote user')
  }
}
</script>

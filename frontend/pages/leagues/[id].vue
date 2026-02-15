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
            <label class="block text-sm font-medium mb-2">Location</label>
            <input v-model="leagueForm.location" type="text"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="e.g. Central Park Sports Centre" />
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Postcode</label>
            <input v-model="leagueForm.postcode" type="text"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"
              placeholder="e.g. M1 3AA" />
            <p class="text-xs text-slate-500 mt-1">Used for league discovery search. UK postcodes only.</p>
          </div>

          <div class="flex items-center gap-3">
            <input v-model="leagueForm.allowRatings" type="checkbox" id="allowRatings"
              class="w-5 h-5 rounded border-gray-300 text-primary focus:ring-primary" />
            <label for="allowRatings" class="text-sm font-medium">Enable Player Ratings</label>
          </div>

          <div class="flex items-center gap-3">
            <input v-model="leagueForm.isPublic" type="checkbox" id="isPublic"
              class="w-5 h-5 rounded border-gray-300 text-primary focus:ring-primary" />
            <label for="isPublic" class="text-sm font-medium">Public League</label>
            <span class="text-xs text-slate-500">(Discoverable via search)</span>
          </div>

          <div>
            <label class="block text-sm font-medium mb-2">Description</label>
            <textarea v-model="leagueForm.description" rows="3"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none"></textarea>
          </div>

          <button type="submit" class="btn-primary">Save Changes</button>
        </form>
      </div>

      <!-- Pending Join Requests -->
      <div v-if="joinRequests.length > 0" class="glass-card p-6 mb-6">
        <h2 class="text-xl font-bold mb-4">
          Pending Join Requests
          <span class="ml-2 px-2 py-0.5 text-xs font-bold bg-amber-500/20 text-amber-400 rounded-full">
            {{ joinRequests.length }}
          </span>
        </h2>
        <div class="space-y-3">
          <div v-for="request in joinRequests" :key="request.id"
            class="flex items-center justify-between p-4 bg-slate-800/50 rounded-lg border border-white/5">
            <div>
              <div class="font-medium">{{ request.fullName }} ({{ request.email }})</div>
              <div class="text-sm text-slate-400">Requested {{ new Date(request.requestedDate).toLocaleDateString() }}
              </div>
            </div>
            <div class="flex items-center gap-2">
              <button @click="handleApproveRequest(request.id)"
                class="px-4 py-2 text-sm font-medium bg-green-600 hover:bg-green-500 text-white rounded-lg transition-colors">
                Approve
              </button>
              <button @click="handleRejectRequest(request.id)"
                class="px-4 py-2 text-sm font-medium bg-red-600/20 hover:bg-red-600/40 text-red-400 rounded-lg transition-colors">
                Reject
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Create League Admin -->
      <div v-if="(data as unknown as AuthSessionData | null)?.user?.roles" class="glass-card p-6 mb-6">
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
              <div class="grid grid-cols-4 gap-2">
                <button v-for="position in availablePositions" :key="position" type="button"
                  @click="toggleAdminPosition(position)" :class="[
                    'py-2 px-1 rounded-lg text-xs font-bold transition-all',
                    adminForm.preferredPosition.includes(position)
                      ? 'bg-primary text-white'
                      : 'bg-slate-800 text-slate-400 hover:bg-slate-700'
                  ]">
                  {{ position }}
                </button>
              </div>
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
          <div v-for="member in members" :key="member.userId"
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
              <button v-if="member.role === 'Member'" @click="handlePromote(member.userId)"
                class="text-sm text-blue-400 hover:text-blue-300">
                Promote
              </button>
              <button v-else-if="member.role === 'Admin'" @click="handleDemote(member.userId)"
                class="text-sm text-orange-400 hover:text-orange-300">
                Demote
              </button>
              <button @click="handleRemove(member.userId)" class="text-sm text-red-400 hover:text-red-300">
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
import type { ApiError } from '~/types/api'
import type { AuthSessionData } from '~/types/auth'
import type { LeagueDetail, LeagueJoinRequest, LeagueMember } from '~/types/league'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()

const { updateLeague, getLeague, getLeagueMembers, addMember, removeMember, promoteToAdmin, demoteAdmin, createLeagueAdmin, getJoinRequests, approveJoinRequest, rejectJoinRequest } = useLeague()
const { data } = useAuth()
const { getPositionsForSport } = useSportPositions()
const modal = useModal()

const leagueId = route.params.id as string
const league = ref<LeagueDetail | null>(null)
const members = ref<LeagueMember[]>([])
const joinRequests = ref<LeagueJoinRequest[]>([])
const newMemberEmail = ref('')

const leagueForm = ref({
  name: '',
  sport: 'Football',
  maxTeams: 2,
  location: '',
  description: '',
  allowRatings: true,
  isPublic: false,
  postcode: ''
})

const adminForm = ref({
  fullName: '',
  email: '',
  password: '',
  preferredPosition: [] as string[]
})

// Get positions based on current league sport
const availablePositions = computed(() => {
  return getPositionsForSport(leagueForm.value.sport)
})

const toggleAdminPosition = (pos: string) => {
  const index = adminForm.value.preferredPosition.indexOf(pos)
  if (index > -1) {
    // Remove
    adminForm.value.preferredPosition.splice(index, 1)
  } else {
    // Add
    adminForm.value.preferredPosition.push(pos)
  }
}

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
        description: league.value.description || '',
        allowRatings: league.value.allowRatings !== false, // Default to true if undefined
        isPublic: league.value.isPublic || false,
        postcode: league.value.postcode || ''
      }
    }

    // Fetch members
    members.value = await getLeagueMembers(leagueId)

    // Fetch join requests
    try {
      joinRequests.value = await getJoinRequests(leagueId)
    } catch (error) {
      // User might not be admin, that's ok
      joinRequests.value = []
    }
  } catch (error) {
    console.error('Failed to load league data:', error)
  }
}

const handleUpdateLeague = async () => {
  try {
    await updateLeague(leagueId, leagueForm.value)
    modal.showInfo(
      `League ${league.value?.name} has been updated successfully!`,
      'Update League'
    )
  } catch (error) {
    console.error('Failed to update league:', error)
    modal.showError('Failed to update league', 'Error')
  }
}

const handleCreateAdmin = async () => {
  try {
    await createLeagueAdmin(leagueId, adminForm.value)
    modal.showInfo('League admin created successfully!', 'Success')
    // Reset form
    adminForm.value = {
      fullName: '',
      email: '',
      password: '',
      preferredPosition: []
    }
    // Reload members list
    await loadLeagueData()
  } catch (error: unknown) {
    const apiErr = error as ApiError
    console.error('Failed to create league admin:', error)
    modal.showError(apiErr.data?.Message || 'Failed to create league admin', 'Error')
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

const handleApproveRequest = async (requestId: string) => {
  try {
    await approveJoinRequest(leagueId, requestId)
    modal.showInfo('Join request approved! Player has been added to the league.', 'Approved')
    await loadLeagueData()
  } catch (error) {
    console.error('Failed to approve request:', error)
    modal.showError('Failed to approve join request', 'Error')
  }
}

const handleRejectRequest = async (requestId: string) => {
  if (confirm('Are you sure you want to reject this join request?')) {
    try {
      await rejectJoinRequest(leagueId, requestId)
      await loadLeagueData()
    } catch (error) {
      console.error('Failed to reject request:', error)
      modal.showError('Failed to reject join request', 'Error')
    }
  }
}
</script>

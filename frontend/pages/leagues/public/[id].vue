<template>
  <div class="container mx-auto px-6 py-12">
    <div class="max-w-2xl mx-auto">
      <div class="mb-8">
        <NuxtLink to="/leagues/search" class="text-sm text-slate-400 hover:text-white mb-4 inline-block">
          ← Back to Search
        </NuxtLink>
      </div>

      <div v-if="loading" class="glass-card p-12 text-center">
        <p class="text-slate-400">Loading league details...</p>
      </div>

      <div v-else-if="league" class="glass-card p-8">
        <div class="mb-6">
          <span class="px-3 py-1 text-xs font-medium bg-slate-700/60 rounded mb-3 inline-block">{{ league.sport }}</span>
          <h1 class="text-3xl font-bold mb-2">{{ league.name }}</h1>
          <div class="flex items-center gap-3 text-sm text-slate-400">
            <span v-if="league.location">📍 {{ league.location }}</span>
            <span v-if="league.postcode">· {{ league.postcode }}</span>
          </div>
        </div>

        <p v-if="league.description" class="text-slate-300 mb-6 leading-relaxed">{{ league.description }}</p>

        <div class="grid grid-cols-2 gap-4 mb-8">
          <div class="bg-slate-800/50 rounded-lg p-4 text-center">
            <div class="text-2xl font-bold text-primary">{{ league.memberCount }}</div>
            <div class="text-xs text-slate-500">Members</div>
          </div>
          <div class="bg-slate-800/50 rounded-lg p-4 text-center">
            <div class="text-2xl font-bold text-primary">{{ league.maxTeams }}</div>
            <div class="text-xs text-slate-500">Teams</div>
          </div>
          <div v-if="league.cost > 0" class="bg-slate-800/50 rounded-lg p-4 text-center">
            <div class="text-2xl font-bold text-primary">£{{ league.cost }}</div>
            <div class="text-xs text-slate-500">Cost</div>
          </div>
        </div>

        <!-- Join Status -->
        <div v-if="league.isAlreadyMember" class="text-center">
          <div class="bg-green-500/10 border border-green-500/20 rounded-lg p-4">
            <p class="text-green-400 font-medium">✓ You're already a member of this league</p>
            <NuxtLink :to="`/leagues/${league.id}`" class="text-sm text-green-300 underline hover:no-underline mt-1 inline-block">
              Go to league →
            </NuxtLink>
          </div>
        </div>

        <div v-else-if="requestSent || league.hasPendingRequest" class="text-center">
          <div class="bg-amber-500/10 border border-amber-500/20 rounded-lg p-4">
            <p class="text-amber-400 font-medium">⏳ Your join request is pending</p>
            <p class="text-sm text-slate-500 mt-1">The league admin will review your request.</p>
          </div>
        </div>

        <div v-else class="text-center">
          <button @click="handleJoin" :disabled="joining" class="btn-primary text-lg px-8 py-3">
            {{ joining ? 'Sending Request...' : 'Request to Join' }}
          </button>
          <p class="text-xs text-slate-500 mt-3">Your request will be reviewed by the league admin.</p>
        </div>
      </div>

      <div v-else class="glass-card p-12 text-center">
        <p class="text-slate-400">League not found or is no longer public.</p>
        <NuxtLink to="/leagues/search" class="btn-primary inline-block mt-4">Back to Search</NuxtLink>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const { getPublicLeague, requestToJoin } = useLeague()
const modal = useModal()

const leagueId = route.params.id as string
const league = ref<any>(null)
const loading = ref(true)
const joining = ref(false)
const requestSent = ref(false)

onMounted(async () => {
  try {
    league.value = await getPublicLeague(leagueId)
  } catch (error) {
    console.error('Failed to fetch league:', error)
  } finally {
    loading.value = false
  }
})

const handleJoin = async () => {
  joining.value = true
  try {
    await requestToJoin(leagueId)
    requestSent.value = true
    modal.showInfo('Your join request has been sent! The league admin will review it.', 'Request Sent')
  } catch (error: any) {
    modal.showError(error.data?.message || 'Failed to send join request', 'Error')
  } finally {
    joining.value = false
  }
}
</script>

<template>
  <div v-if="userLeagues.length > 0" class="flex items-center gap-3 w-full">
    <div v-if="userLeagues.length > 1" class="relative w-full">
      <select v-model="selectedLeagueId" @change="onLeagueChange"
        class="w-full appearance-none px-4 py-2 rounded-xl bg-slate-800 border border-white/10 text-white text-sm font-medium focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/20 transition-all cursor-pointer">
        <option v-for="league in userLeagues" :key="league.id" :value="league.id" class="bg-slate-800 text-white">
          {{ league.name }} ({{ league.sport }})
        </option>
      </select>
      <div class="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none text-slate-400">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
          stroke-linecap="round" stroke-linejoin="round">
          <path d="M6 9l6 6 6-6" />
        </svg>
      </div>
    </div>
    <div v-else>
      <p class="text-slate-500 text-xs font-bold uppercase tracking-widest">{{ userLeagues[0].name }} ({{
        userLeagues[0].sport }})</p>
    </div>
  </div>
</template>

<script setup lang="ts">
const { currentLeague, userLeagues, setCurrentLeague, fetchUserLeagues } = useLeague()
const { data } = useAuth()
const router = useRouter()

const selectedLeagueId = ref(currentLeague.value?.id || '')

// Fetch leagues on mount
onMounted(async () => {
  await fetchUserLeagues()
  if (currentLeague.value) {
    selectedLeagueId.value = currentLeague.value.id
  }
})

const onLeagueChange = async () => {
  const league = userLeagues.value.find((l: any) => l.id === selectedLeagueId.value)
  if (league) {
    setCurrentLeague(league)
    // Navigate to current route to trigger data refresh without full page reload
    await navigateTo(router.currentRoute.value.fullPath, { replace: true })
  }
}

// Watch for changes to currentLeague from other sources
watch(currentLeague, (newLeague) => {
  if (newLeague) {
    selectedLeagueId.value = newLeague.id
  }
})
</script>

<template>
  <div v-if="userLeagues.length > 0" class="league-selector">
    <select v-model="selectedLeagueId" @change="onLeagueChange" class="league-select text-black">
      <option class="text-black" v-for="league in userLeagues" :key="league.id" :value="league.id">
        {{ league.name }} ({{ league.sport }})
      </option>
    </select>

    <button v-if="isSuperAdmin" @click="$router.push('/leagues')" class="create-league-btn">
      + Create League
    </button>
  </div>
</template>

<script setup lang="ts">
const { currentLeague, userLeagues, setCurrentLeague, fetchUserLeagues } = useLeague()
const { data } = useAuth()
const router = useRouter()

const selectedLeagueId = ref(currentLeague.value?.id || '')

// Check if user is super admin from token claims
const isSuperAdmin = computed(() => (data.value as any)?.user?.isSuperAdmin === 'True')

// Fetch leagues on mount
onMounted(async () => {
  await fetchUserLeagues()
  if (currentLeague.value) {
    selectedLeagueId.value = currentLeague.value.id
  }
})

const onLeagueChange = () => {
  const league = userLeagues.value.find((l: any) => l.id === selectedLeagueId.value)
  if (league) {
    setCurrentLeague(league)
    // Refresh current page to reload data with new league context
    router.go(0)
  }
}

// Watch for changes to currentLeague from other sources
watch(currentLeague, (newLeague) => {
  if (newLeague) {
    selectedLeagueId.value = newLeague.id
  }
})
</script>

<style scoped>
.league-selector {
  display: flex;
  gap: 12px;
  align-items: center;
}

.league-select {
  padding: 8px 16px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  background: white;
  cursor: pointer;
  min-width: 200px;
}

.league-select:hover {
  border-color: #3b82f6;
}

.league-select:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.create-league-btn {
  padding: 8px 16px;
  background: #3b82f6;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.create-league-btn:hover {
  background: #2563eb;
}
</style>

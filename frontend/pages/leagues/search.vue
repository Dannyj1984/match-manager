<template>
  <div class="container mx-auto px-6 py-12">
    <div class="max-w-4xl mx-auto">
      <div class="mb-8">
        <NuxtLink to="/leagues" class="text-sm text-slate-400 hover:text-white mb-4 inline-block">
          ← Back to My Leagues
        </NuxtLink>
        <h1 class="text-3xl font-bold mb-2">Find Leagues</h1>
        <p class="text-slate-400">Search for public leagues near you by postcode.</p>
      </div>

      <!-- Search Form -->
      <div class="glass-card p-6 mb-8">
        <form @submit.prevent="handleSearch" class="flex flex-col sm:flex-row gap-4">
          <div class="flex-1">
            <label class="block text-sm font-medium mb-2">Postcode</label>
            <input v-model="searchPostcode" type="text" required placeholder="e.g. SK15 3DT"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none" />
          </div>
          <div class="sm:w-48">
            <label class="block text-sm font-medium mb-2">Distance</label>
            <select v-model.number="searchRadius"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
              <option :value="1">1 mile</option>
              <option :value="3">3 miles</option>
              <option :value="5">5 miles</option>
              <option :value="10">10 miles</option>
              <option :value="15">15 miles</option>
              <option :value="25">25 miles</option>
            </select>
          </div>
          <div class="sm:w-48">
            <label class="block text-sm font-medium mb-2">Sport</label>
            <select v-model="searchSport"
              class="w-full px-4 py-2 rounded-lg bg-slate-800 border border-white/10 focus:border-primary outline-none">
              <option value="Any">Any Sport</option>
              <option value="Football">Football</option>
              <option value="Netball">Netball</option>
              <option value="Basketball">Basketball</option>
              <option value="Rugby">Rugby</option>
            </select>
          </div>
          <div class="sm:self-end">
            <button type="submit" class="btn-primary w-full sm:w-auto" :disabled="searching">
              {{ searching ? 'Searching...' : 'Search' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Search Results -->
      <div v-if="hasSearched">
        <h2 class="text-xl font-bold mb-4">
          <span v-if="results.length === 1 && results[0].distanceMiles > searchRadius" class="text-amber-400">
            No leagues found within {{ searchRadius }} miles. Nearest league:
          </span>
          <span v-else>
            {{ results.length }} league{{ results.length !== 1 ? 's' : '' }} found
          </span>
        </h2>

        <div v-if="results.length > 0" class="grid gap-4">
          <NuxtLink v-for="league in results" :key="league.id" :to="`/leagues/public/${league.id}`"
            class="glass-card p-6 hover:border-primary/30 transition-colors cursor-pointer block">
            <div class="flex justify-between items-start">
              <div>
                <h3 class="text-lg font-bold mb-1">{{ league.name }}</h3>
                <div class="flex flex-wrap items-center gap-2 text-sm text-slate-400">
                  <span class="px-2 py-0.5 rounded bg-slate-700/60 text-xs font-medium">{{ league.sport }}</span>
                  <span v-if="league.location">{{ league.location }}</span>
                  <span v-if="league.postcode">· {{ league.postcode }}</span>
                </div>
                <p v-if="league.description" class="text-sm text-slate-500 mt-2 line-clamp-2">{{ league.description }}</p>
              </div>
              <div class="text-right shrink-0 ml-4">
                <div class="text-lg font-bold text-primary">{{ league.distanceMiles }} mi</div>
                <div class="text-xs text-slate-500">{{ league.memberCount }} member{{ league.memberCount !== 1 ? 's' : '' }}</div>
                <div v-if="league.isAlreadyMember" class="mt-2">
                  <span class="px-2 py-1 text-xs font-medium bg-green-500/20 text-green-400 rounded">Member</span>
                </div>
                <div v-else-if="league.hasPendingRequest" class="mt-2">
                  <span class="px-2 py-1 text-xs font-medium bg-amber-500/20 text-amber-400 rounded">Pending</span>
                </div>
              </div>
            </div>
          </NuxtLink>
        </div>

        <div v-else class="glass-card p-12 text-center">
          <p class="text-slate-400">No public leagues found within {{ searchRadius }} mile{{ searchRadius !== 1 ? 's' : '' }} of {{ searchPostcode }}{{ searchSport !== 'Any' ? ` for ${searchSport}` : '' }}.</p>
          <p class="text-sm text-slate-500 mt-2">Try increasing the search radius, changing the sport, or using a different postcode.</p>
        </div>
      </div>

      <!-- Initial State -->
      <div v-else class="glass-card p-12 text-center">
        <div class="text-6xl mb-4">🔍</div>
        <p class="text-slate-400">Enter your postcode above to find leagues near you.</p>
      </div>

      <!-- Error State -->
      <div v-if="searchError" class="glass-card p-6 border-red-500/30 mt-4">
        <p class="text-red-400">{{ searchError }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: 'auth'
})

const { searchLeagues } = useLeague()

const searchPostcode = ref('')
const searchRadius = ref(5)
const searchSport = ref('Any')
const results = ref<any[]>([])
const hasSearched = ref(false)
const searching = ref(false)
const searchError = ref('')

const handleSearch = async () => {
  if (!searchPostcode.value.trim()) return

  searching.value = true
  searchError.value = ''
  
  try {
    results.value = await searchLeagues(searchPostcode.value.trim(), searchRadius.value, searchSport.value)
    hasSearched.value = true
  } catch (error: any) {
    searchError.value = error.data?.message || 'Failed to search leagues. Please check the postcode and try again.'
    results.value = []
    hasSearched.value = true
  } finally {
    searching.value = false
  }
}
</script>

<script setup lang="ts">
import { LogIn, Mail, Lock, ShieldCheck, Loader2 } from 'lucide-vue-next'

const { signIn } = useAuth()
const router = useRouter()

const form = reactive({
  email: '',
  password: ''
})

const isLoading = ref(false)
const error = ref('')

const handleLogin = async () => {
  isLoading.value = true
  error.value = ''

  try {
    const result = await signIn(form, { redirect: false })
    if (result?.error) {
      error.value = 'Invalid email or password'
    } else {
      router.push('/')
    }
  } catch (err) {
    error.value = 'An error occurred during login'
  } finally {
    isLoading.value = false
  }
}

definePageMeta({
  layout: false,
  middleware: 'guest-only'
})
</script>

<template>
  <div class="min-h-screen bg-background flex items-center justify-center p-4 selection:bg-primary/30">
    <!-- background ornaments -->
    <div class="fixed inset-0 pointer-events-none -z-10 overflow-hidden">
      <div class="absolute -top-[20%] -left-[10%] w-[50%] h-[50%] bg-primary/10 blur-[120px] rounded-full"></div>
      <div class="absolute -bottom-[20%] -right-[10%] w-[40%] h-[40%] bg-blue-600/5 blur-[100px] rounded-full"></div>
    </div>

    <div class="w-full max-w-md space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div class="text-center space-y-2">
        <div class="flex justify-center mb-6">
          <div
            class="w-16 h-16 rounded-2xl bg-primary shadow-2xl shadow-primary/30 flex items-center justify-center text-white">
            <ShieldCheck :size="32" />
          </div>
        </div>
        <h1 class="text-4xl font-black text-white tracking-tighter italic">FAIR<span class="text-primary">PLAY</span>
        </h1>
        <p class="text-slate-400 font-medium">Login</p>
      </div>

      <div class="glass-card p-8 border-slate-800 shadow-2xl">
        <form @submit.prevent="handleLogin" class="space-y-6">
          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-500 uppercase tracking-widest px-1">Email Address</label>
            <div class="relative group">
              <Mail
                class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-primary transition-colors"
                :size="18" />
              <input v-model="form.email" type="email" required placeholder="admin@eventplay.com"
                class="w-full bg-slate-900/50 border border-white/5 rounded-xl py-4 pl-12 pr-4 text-white focus:outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/10 transition-all">
            </div>
          </div>

          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-500 uppercase tracking-widest px-1">Password</label>
            <div class="relative group">
              <Lock
                class="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-primary transition-colors"
                :size="18" />
              <input v-model="form.password" type="password" required placeholder="••••••••"
                class="w-full bg-slate-900/50 border border-white/5 rounded-xl py-4 pl-12 pr-4 text-white focus:outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/10 transition-all">
            </div>
          </div>

          <p v-if="error"
            class="text-danger text-sm font-bold text-center px-4 py-2 bg-danger/10 border border-danger/20 rounded-lg animate-shake">
            {{ error }}
          </p>

          <button type="submit" :disabled="isLoading"
            class="w-full btn-primary py-4 flex items-center justify-center gap-2 group">
            <Loader2 v-if="isLoading" class="animate-spin" :size="20" />
            <span v-else class="flex items-center gap-2">
              LOG IN
              <LogIn :size="20" class="group-hover:translate-x-1 transition-transform" />
            </span>
          </button>
        </form>

        <div class="mt-8 pt-6 border-t border-white/5 text-center">
          <p class="text-slate-500 text-sm">
            Don't have an account? <a href="#" class="text-primary font-bold hover:underline">Register as a player</a>
          </p>
        </div>
      </div>

      <p class="text-center text-[10px] text-slate-600 uppercase tracking-[0.2em] font-bold">
        Secure Access • EvenPlay Pro v1.0
      </p>
    </div>
  </div>
</template>

<style scoped>
.animate-shake {
  animation: shake 0.5s cubic-bezier(.36, .07, .19, .97) both;
}

@keyframes shake {

  10%,
  90% {
    transform: translate3d(-1px, 0, 0);
  }

  20%,
  80% {
    transform: translate3d(2px, 0, 0);
  }

  30%,
  50%,
  70% {
    transform: translate3d(-4px, 0, 0);
  }

  40%,
  60% {
    transform: translate3d(4px, 0, 0);
  }
}
</style>

<script setup lang="ts">
const props = defineProps<{
  playerName: string
  modelValue: number
}>()

const emit = defineEmits(['update:modelValue'])

const updateValue = (e: Event) => {
  const target = e.target as HTMLInputElement
  emit('update:modelValue', parseInt(target.value))
}

const getRatingColor = (val: number) => {
  if (val <= 4) return 'text-danger'
  if (val <= 7) return 'text-warning'
  return 'text-success'
}
</script>

<template>
  <div class="glass-card p-4 space-y-3">
    <div class="flex justify-between items-center">
      <span class="font-bold text-slate-200">{{ playerName }}</span>
      <span :class="['text-xl font-black transition-colors', getRatingColor(modelValue)]">
        {{ modelValue }}
      </span>
    </div>
    
    <div class="relative h-6 flex items-center">
      <input 
        type="range" 
        min="1" 
        max="10" 
        step="1"
        :value="modelValue"
        @input="updateValue"
        class="w-full h-1.5 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-primary"
      >
      <div class="absolute -bottom-1 w-full flex justify-between px-0.5 pointer-events-none">
        <span v-for="i in 10" :key="i" class="w-0.5 h-1 bg-slate-700"></span>
      </div>
    </div>
    <div class="flex justify-between text-[10px] uppercase font-bold text-slate-600 tracking-tighter">
      <span>1</span>
      <span>5</span>
      <span>10</span>
    </div>
  </div>
</template>

<style scoped>
input[type=range]::-webkit-slider-runnable-track {
  @apply rounded-lg;
}
input[type=range]::-webkit-slider-thumb {
  -webkit-appearance: none;
  @apply w-5 h-5 bg-white border-2 border-primary rounded-full shadow-lg -mt-1.5 transition-transform active:scale-125;
}
</style>

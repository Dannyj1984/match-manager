<script setup lang="ts">
import { X, AlertCircle, CheckCircle2 } from 'lucide-vue-next'

interface Props {
    isOpen: boolean
    title?: string
    message: string
    type?: 'confirm' | 'info' | 'success' | 'error'
    confirmText?: string
    cancelText?: string
}

const props = withDefaults(defineProps<Props>(), {
    type: 'info',
    confirmText: 'Confirm',
    cancelText: 'Cancel'
})

const emit = defineEmits<{
    confirm: []
    cancel: []
    close: []
}>()

const handleConfirm = () => {
    emit('confirm')
    emit('close')
}

const handleCancel = () => {
    emit('cancel')
    emit('close')
}

const getIcon = () => {
    switch (props.type) {
        case 'success':
            return CheckCircle2
        case 'error':
        case 'confirm':
            return AlertCircle
        default:
            return AlertCircle
    }
}

const getIconColor = () => {
    switch (props.type) {
        case 'success':
            return 'text-green-400'
        case 'error':
            return 'text-red-400'
        case 'confirm':
            return 'text-yellow-400'
        default:
            return 'text-blue-400'
    }
}
</script>

<template>
    <Teleport to="body">
        <Transition name="modal">
            <div v-if="isOpen" class="fixed inset-0 z-[9999] flex items-center justify-center p-4"
                @click.self="type !== 'confirm' ? handleCancel() : null">
                <!-- Backdrop -->
                <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"></div>

                <!-- Modal -->
                <div class="relative glass-card max-w-md w-full p-6 animate-modal-in">
                    <!-- Close button (only for info/success/error) -->
                    <button v-if="type !== 'confirm'" @click="handleCancel"
                        class="absolute top-4 right-4 text-slate-400 hover:text-white transition-colors">
                        <X :size="20" />
                    </button>

                    <!-- Icon and Title -->
                    <div class="flex items-start gap-4 mb-4">
                        <div :class="['flex-shrink-0 p-2 rounded-xl bg-white/5', getIconColor()]">
                            <component :is="getIcon()" :size="24" />
                        </div>
                        <div class="flex-1">
                            <h3 v-if="title" class="text-lg font-bold mb-2">{{ title }}</h3>
                            <p class="text-slate-300 leading-relaxed">{{ message }}</p>
                        </div>
                    </div>

                    <!-- Actions -->
                    <div v-if="type === 'confirm'" class="flex gap-3 mt-6">
                        <button @click="handleCancel"
                            class="flex-1 px-4 py-2.5 rounded-lg bg-white/5 hover:bg-white/10 text-slate-300 hover:text-white font-bold transition-all">
                            {{ cancelText }}
                        </button>
                        <button @click="handleConfirm"
                            class="flex-1 px-4 py-2.5 rounded-lg bg-primary hover:bg-primary-dark text-white font-bold transition-all shadow-lg shadow-primary/20">
                            {{ confirmText }}
                        </button>
                    </div>
                    <div v-else class="flex justify-end mt-6">
                        <button @click="handleCancel"
                            class="px-6 py-2.5 rounded-lg bg-primary hover:bg-primary-dark text-white font-bold transition-all shadow-lg shadow-primary/20">
                            OK
                        </button>
                    </div>
                </div>
            </div>
        </Transition>
    </Teleport>
</template>

<style scoped>
.modal-enter-active,
.modal-leave-active {
    transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
    opacity: 0;
}

@keyframes modal-in {
    from {
        opacity: 0;
        transform: scale(0.95) translateY(-10px);
    }

    to {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

.animate-modal-in {
    animation: modal-in 0.2s ease-out;
}
</style>

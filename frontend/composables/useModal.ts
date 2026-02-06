// Global resolver that's shared across all instances
let globalResolvePromise: ((value: boolean) => void) | null = null

export const useModal = () => {
    // Use Nuxt's useState to create a global shared state
    const isOpen = useState('modal-isOpen', () => false)
    const modalConfig = useState('modal-config', () => ({
        title: '',
        message: '',
        type: 'info' as 'confirm' | 'info' | 'success' | 'error',
        confirmText: 'Confirm',
        cancelText: 'Cancel'
    }))

    const showConfirm = (message: string, title?: string, confirmText = 'Confirm', cancelText = 'Cancel'): Promise<boolean> => {
        return new Promise((resolve) => {
            modalConfig.value = {
                title: title || 'Confirm Action',
                message,
                type: 'confirm',
                confirmText,
                cancelText
            }
            isOpen.value = true
            globalResolvePromise = resolve
        })
    }

    const showInfo = (message: string, title?: string) => {
        modalConfig.value = {
            title: title || 'Information',
            message,
            type: 'info',
            confirmText: 'OK',
            cancelText: 'Cancel'
        }
        isOpen.value = true
    }

    const showSuccess = (message: string, title?: string) => {
        modalConfig.value = {
            title: title || 'Success',
            message,
            type: 'success',
            confirmText: 'OK',
            cancelText: 'Cancel'
        }
        isOpen.value = true
    }

    const showError = (message: string, title?: string) => {
        modalConfig.value = {
            title: title || 'Error',
            message,
            type: 'error',
            confirmText: 'OK',
            cancelText: 'Cancel'
        }
        isOpen.value = true
    }

    const handleConfirm = () => {
        if (globalResolvePromise) {
            globalResolvePromise(true)
            globalResolvePromise = null
        }
        isOpen.value = false
    }

    const handleCancel = () => {
        if (globalResolvePromise) {
            globalResolvePromise(false)
            globalResolvePromise = null
        }
        isOpen.value = false
    }

    const close = () => {
        handleCancel()
    }

    return {
        isOpen,
        modalConfig,
        showConfirm,
        showInfo,
        showSuccess,
        showError,
        handleConfirm,
        handleCancel,
        close
    }
}

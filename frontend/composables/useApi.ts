export const useApi = () => {
    const { token } = useAuth()
    const config = useRuntimeConfig()
    const currentLeague = useState<any>('currentLeague')

    const fetch = async <T>(path: string, options: any = {}) => {

        const headers = {
            ...options.headers
        } as Record<string, string>

        if (token.value) {
            headers['Authorization'] = token.value
        }

        // Automatically inject league ID header if current league is set
        if (currentLeague.value?.id && !headers['X-League-Id']) {
            headers['X-League-Id'] = currentLeague.value.id
        }

        return await $fetch<T>(path, {
            baseURL: config.public.apiEndpoint,
            ...options,
            headers,
            onResponseError({ response }) {
                console.error('[API Error]', response._data || response.statusText)
            }
        })
    }

    return {
        fetch
    }
}

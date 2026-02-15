/** Shape of error responses from $fetch / FetchError */
export interface ApiError {
    data?: {
        message?: string
        Message?: string
    }
}

/** Options passed to the useApi fetch wrapper */
export interface ApiRequestOptions {
    method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'HEAD' | 'OPTIONS'
    headers?: Record<string, string>
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    body?: Record<string, any> | any[] | BodyInit | null
    [key: string]: unknown
}

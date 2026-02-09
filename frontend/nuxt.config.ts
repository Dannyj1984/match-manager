// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },
  modules: [
    '@nuxtjs/tailwindcss',
    '@pinia/nuxt',
    '@sidebase/nuxt-auth'
  ],
  auth: {
    origin: process.env.AUTH_ORIGIN || 'http://localhost:3000',
    baseURL: '/api/auth',
    provider: {
      type: 'local',
      endpoints: {
        signIn: { path: '/api/auth/login', method: 'post' },
        signUp: { path: '/api/auth/register', method: 'post' },
        getSession: { path: '/api/auth/me', method: 'get' },
        signOut: false
      },
      token: {
        signInResponseTokenPointer: '/token',
        type: 'Bearer',
        headerName: 'Authorization'
      }
    },
    globalAppMiddleware: false
  },
  nitro: {
    routeRules: {
      '/api/**': { proxy: process.env.NUXT_API_PROXY_TARGET || 'http://127.0.0.1:5137/api/**' }
    }
  },
  runtimeConfig: {
    public: {
      apiEndpoint: '/api'
    }
  },
  app: {
    head: {
      title: 'EvenPlay',
      meta: [
        { name: 'viewport', content: 'width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0' },
        { name: 'description', content: 'Fair team balancing for competitive social football.' }
      ],
      link: [
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap' }
      ]
    }
  }
})

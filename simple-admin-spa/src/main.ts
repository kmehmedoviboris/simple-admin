import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import 'primeicons/primeicons.css'

import App from './App.vue'
import router from './router'
import { client } from '@/client/client.gen'
import { useAuthStore } from '@/stores/authStore'

const app = createApp(App)
const pinia = createPinia()

// Registration order is critical:
// 1. Pinia FIRST — so stores are available in router beforeEach guards
// 2. Router SECOND
// 3. PrimeVue THIRD
app.use(pinia)
app.use(router)
app.use(PrimeVue, { theme: { preset: Aura } })

// Configure hey-api client with Bearer token injection and 401 handling
client.interceptors.request.use((request) => {
  const auth = useAuthStore()
  if (auth.accessToken) {
    request.headers.set('Authorization', `Bearer ${auth.accessToken}`)
  }
  return request
})

client.interceptors.response.use(async (response) => {
  if (response.status === 401) {
    const auth = useAuthStore()
    auth.setUser(null)
    await auth.login() // triggers signinRedirect — clears tokens and redirects
  }
  return response
})

app.mount('#app')

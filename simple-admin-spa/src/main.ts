import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import 'primeicons/primeicons.css'

import App from './App.vue'
import router from './router'

const app = createApp(App)
const pinia = createPinia()

// Registration order is critical:
// 1. Pinia FIRST — so stores are available in router beforeEach guards
// 2. Router SECOND
// 3. PrimeVue THIRD
app.use(pinia)
app.use(router)
app.use(PrimeVue, { theme: { preset: Aura } })

app.mount('#app')

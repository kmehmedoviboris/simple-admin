import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/callback',
      component: () => import('@/views/CallbackView.vue'),
      meta: { public: true },
    },
    {
      path: '/oidc-error',
      component: () => import('@/views/OidcErrorView.vue'),
      meta: { public: true },
    },
    {
      path: '/users',
      component: () => import('@/views/UsersView.vue'),
    },
    {
      path: '/',
      redirect: '/users',
    },
  ],
})

router.beforeEach(async (to) => {
  if (to.meta.public) return true

  // CRITICAL: call useAuthStore() inside the guard body, not at module top level.
  // Even though useAuthStore is imported at the top, calling it here is safe because
  // by the time beforeEach runs, app.use(pinia) has already completed.
  const auth = useAuthStore()

  if (!auth.user) await auth.initialize()

  if (!auth.isAuthenticated) {
    await auth.login() // triggers signinRedirect — never returns
    return false
  }

  return true
})

export default router

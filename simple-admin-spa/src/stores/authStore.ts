import { defineStore } from 'pinia'
import { userManager } from '@/auth/oidcManager'
import type { User } from 'oidc-client-ts'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
  }),
  getters: {
    isAuthenticated: (state) => !!state.user && !state.user.expired,
    accessToken: (state) => state.user?.access_token ?? null,
  },
  actions: {
    async initialize() {
      this.user = await userManager.getUser()
    },
    async login() {
      await userManager.signinRedirect()
    },
    async logout() {
      await userManager.signoutRedirect()
    },
    setUser(user: User | null) {
      this.user = user
    },
  },
})

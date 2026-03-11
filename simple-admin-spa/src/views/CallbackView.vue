<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { userManager } from '@/auth/oidcManager'
import { useAuthStore } from '@/stores/authStore'

const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  try {
    const user = await userManager.signinRedirectCallback()
    auth.setUser(user)
    await router.replace('/users')
  } catch (err) {
    const reason = err instanceof Error ? err.message : String(err)
    await router.replace({ path: '/oidc-error', query: { reason } })
  }
})
</script>

<template>
  <div>Completing login...</div>
</template>

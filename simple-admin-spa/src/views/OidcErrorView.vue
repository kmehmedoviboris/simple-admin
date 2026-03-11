<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import Button from 'primevue/button'

const route = useRoute()
const auth = useAuthStore()

const reason = route.query.reason as string | undefined
</script>

<template>
  <div class="oidc-error">
    <h1>Authentication Error</h1>
    <p v-if="reason">{{ reason }}</p>
    <p v-else>An unexpected error occurred during login.</p>
    <Button label="Try again" @click="auth.login()" />
  </div>
</template>

<style scoped>
.oidc-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  gap: 1rem;
}
</style>

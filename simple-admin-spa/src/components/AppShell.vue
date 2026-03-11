<script setup lang="ts">
import Toolbar from 'primevue/toolbar'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/authStore'

const auth = useAuthStore()
</script>

<template>
  <div class="app-shell">
    <Toolbar>
      <template #start>
        <span class="app-title">SimpleAdmin</span>
      </template>
      <template #end>
        <Button label="Logout" @click="auth.logout()" />
      </template>
    </Toolbar>
    <main>
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app-title {
  font-size: 1.25rem;
  font-weight: 600;
}

main {
  padding: 1rem;
}
</style>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import AppShell from '@/components/AppShell.vue'
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'

const route = useRoute()
const auth = useAuthStore()

// Show AppShell only on authenticated, non-public routes
const showShell = computed(() => !route.meta.public && auth.isAuthenticated)
</script>

<template>
  <Toast position="top-right" />
  <ConfirmDialog />
  <AppShell v-if="showShell">
    <RouterView />
  </AppShell>
  <RouterView v-else />
</template>

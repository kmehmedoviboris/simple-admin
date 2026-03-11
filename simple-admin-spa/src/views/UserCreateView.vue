<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { postApiUsers } from '@/client/sdk.gen'
import UserForm from '@/components/UserForm.vue'

const router = useRouter()
const toast = useToast()
const loading = ref(false)

async function onSubmit({ email, password }: { email: string; password: string }) {
  loading.value = true
  const { error } = await postApiUsers({ body: { email, password } })
  loading.value = false
  if (error) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: (error as any)?.detail ?? 'Failed to create user',
      life: 3000,
    })
    return
  }
  toast.add({ severity: 'success', summary: 'Created', detail: 'User created', life: 3000 })
  router.push('/users')
}
</script>

<template>
  <div class="p-4">
    <h1>Create User</h1>
    <UserForm :is-create="true" :loading="loading" @submit="onSubmit" />
  </div>
</template>

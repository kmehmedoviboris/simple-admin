<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { getApiUsers, putApiUsersById } from '@/client/sdk.gen'
import UserForm from '@/components/UserForm.vue'

const router = useRouter()
const route = useRoute()
const toast = useToast()

const userId = route.params.id as string
const loading = ref(false)
const pageLoading = ref(false)
const userEmail = ref('')

onMounted(async () => {
  pageLoading.value = true
  const { data, error } = await getApiUsers()
  pageLoading.value = false
  if (error || !data) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: 'Failed to load user',
      life: 3000,
    })
    router.push('/users')
    return
  }
  const found = data.find((u) => u.id === userId)
  if (!found) {
    toast.add({
      severity: 'error',
      summary: 'Not Found',
      detail: 'User not found',
      life: 3000,
    })
    router.push('/users')
    return
  }
  userEmail.value = found.email
})

async function onSubmit({ email, password }: { email: string; password: string }) {
  loading.value = true
  const { error } = await putApiUsersById({
    path: { id: userId },
    body: { email, newPassword: password || null },
  })
  loading.value = false
  if (error) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: (error as any)?.detail ?? 'Failed to update user',
      life: 3000,
    })
    return
  }
  toast.add({ severity: 'success', summary: 'Updated', detail: 'User updated', life: 3000 })
  router.push('/users')
}
</script>

<template>
  <div class="p-4">
    <h1>Edit User</h1>
    <div v-if="pageLoading" class="flex justify-center p-8">
      <i class="pi pi-spinner pi-spin" style="font-size: 2rem"></i>
    </div>
    <UserForm v-else :is-create="false" :initial-email="userEmail" :loading="loading" @submit="onSubmit" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { getApiUsers, getApiMe, deleteApiUsersById } from '@/client/sdk.gen'
import type { UserListDto } from '@/client/types.gen'

const router = useRouter()
const toast = useToast()
const confirm = useConfirm()

const users = ref<UserListDto[]>([])
const loading = ref(false)
const currentUserEmail = ref<string | null>(null)

async function fetchUsers() {
  loading.value = true
  try {
    const { data, error } = await getApiUsers()
    if (error) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: (error as any)?.detail ?? 'Failed to load users',
        life: 3000,
      })
    } else {
      users.value = data ?? []
    }
  } catch (e) {
    toast.add({
      severity: 'error',
      summary: 'Error',
      detail: 'Failed to load users',
      life: 3000,
    })
  } finally {
    loading.value = false
  }
}

async function fetchCurrentUser() {
  try {
    const { data } = await getApiMe()
    const me = data as { sub?: string; email?: string }
    currentUserEmail.value = me?.email ?? null
  } catch {
    // Non-critical — silently ignore
  }
}

function confirmDelete(user: UserListDto) {
  confirm.require({
    message: 'Are you sure you want to delete this user?',
    header: 'Delete User',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Cancel',
    acceptLabel: 'Delete',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        const { error } = await deleteApiUsersById({ path: { id: user.id } })
        if (error) {
          toast.add({
            severity: 'error',
            summary: 'Error',
            detail: (error as any)?.detail ?? 'Failed to delete user',
            life: 3000,
          })
        } else {
          toast.add({
            severity: 'success',
            summary: 'Success',
            detail: 'User deleted',
            life: 3000,
          })
          users.value = users.value.filter((u) => u.id !== user.id)
        }
      } catch {
        toast.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to delete user',
          life: 3000,
        })
      }
    },
  })
}

onMounted(() => {
  fetchUsers()
  fetchCurrentUser()
})
</script>

<template>
  <div>
    <h1>Users</h1>
    <div class="flex justify-end mb-4">
      <Button
        icon="pi pi-plus"
        label="Create User"
        @click="router.push('/users/new')"
      />
    </div>
    <DataTable :value="users" :loading="loading">
      <Column field="email" header="Email" />
      <Column header="Actions">
        <template #body="{ data: row }">
          <Button
            icon="pi pi-pencil"
            severity="info"
            text
            rounded
            @click="router.push('/users/' + row.id + '/edit')"
          />
          <Button
            icon="pi pi-trash"
            severity="danger"
            text
            rounded
            :disabled="row.email === currentUserEmail"
            @click="confirmDelete(row)"
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'

interface Props {
  initialEmail?: string
  initialPassword?: string
  isCreate: boolean
  loading: boolean
}

const props = withDefaults(defineProps<Props>(), {
  initialEmail: '',
  initialPassword: '',
})

const emit = defineEmits<{
  submit: [value: { email: string; password: string }]
}>()

const router = useRouter()

const email = ref(props.initialEmail)
const password = ref(props.initialPassword)
const errors = ref<Record<string, string>>({})

watch(
  () => props.initialEmail,
  (val) => {
    email.value = val ?? ''
  },
)

function validate(): boolean {
  errors.value = {}
  if (!email.value.trim()) {
    errors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    errors.value.email = 'Enter a valid email address'
  }
  if (props.isCreate && !password.value) {
    errors.value.password = 'Password is required'
  }
  return Object.keys(errors.value).length === 0
}

function onSubmit() {
  if (!validate()) return
  emit('submit', { email: email.value, password: password.value })
}
</script>

<template>
  <form @submit.prevent="onSubmit">
    <div class="flex flex-col gap-2 mb-4">
      <label for="email">Email</label>
      <InputText id="email" v-model="email" :invalid="!!errors.email" fluid />
      <Message v-if="errors.email" severity="error" size="small" variant="simple">{{ errors.email }}</Message>
    </div>
    <div class="flex flex-col gap-2 mb-4">
      <label for="password">Password{{ isCreate ? '' : ' (leave blank to keep current)' }}</label>
      <Password id="password" v-model="password" :invalid="!!errors.password" :feedback="false" fluid toggleMask />
      <Message v-if="errors.password" severity="error" size="small" variant="simple">{{ errors.password }}</Message>
    </div>
    <div class="flex gap-2 mt-4">
      <Button :label="isCreate ? 'Create' : 'Save'" type="submit" :loading="loading" />
      <Button label="Cancel" severity="secondary" text @click="router.push('/users')" />
    </div>
  </form>
</template>

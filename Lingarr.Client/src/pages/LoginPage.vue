<template>
    <div class="flex min-h-screen items-center justify-center px-4">
        <div class="w-full max-w-sm">
            <CardComponent title="Sign in to Lingarr">
                <template #content>
                    <div
                        v-if="error"
                        class="mb-6 rounded-md border border-red-700/50 bg-red-900/20 p-4">
                        <p class="text-sm text-red-400">{{ error }}</p>
                    </div>

                    <form class="space-y-6" @submit.prevent="handleLogin">
                        <div>
                            <label for="username" class="mb-1 block text-sm">Username</label>
                            <input
                                id="username"
                                v-model="username"
                                type="text"
                                autocomplete="username"
                                placeholder="Enter your username"
                                class="border-accent w-full rounded-md border bg-transparent px-4 py-2 outline-hidden transition-colors duration-200"
                                required />
                        </div>

                        <div>
                            <label for="password" class="mb-1 block text-sm">Password</label>
                            <input
                                id="password"
                                v-model="password"
                                type="password"
                                autocomplete="current-password"
                                placeholder="Enter your password"
                                class="border-accent w-full rounded-md border bg-transparent px-4 py-2 outline-hidden transition-colors duration-200"
                                required />
                        </div>

                        <div class="flex w-full items-center justify-end">
                            <ButtonComponent type="submit" :disabled="loading" variant="secondary">
                                <LoaderCircleIcon
                                    v-if="loading"
                                    class="mr-2 h-4 w-4 animate-spin" />
                                {{ loading ? 'Signing in...' : 'Sign In' }}
                            </ButtonComponent>
                        </div>
                    </form>
                </template>
            </CardComponent>
        </div>
    </div>
</template>
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import services from '@/services'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import CardComponent from '@/components/common/CardComponent.vue'

const router = useRouter()

const loading = ref(false)
const error = ref('')

const username = ref('')
const password = ref('')

const handleLogin = async () => {
    if (!username.value || !password.value) {
        error.value = 'Please enter your username and password'
        return
    }

    loading.value = true
    error.value = ''

    try {
        await services.auth.login({
            username: username.value,
            password: password.value
        })

        router.push('/')
    } catch (err: any) {
        console.error('Login error:', err)
        error.value = err?.data?.message || 'Invalid username or password'
    } finally {
        loading.value = false
    }
}
</script>

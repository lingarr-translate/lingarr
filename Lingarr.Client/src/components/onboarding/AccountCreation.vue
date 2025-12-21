<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Create an account</h2>
            <p class="mb-6 text-sm text-gray-400">
                Enter your credentials to create the administrator account.
            </p>
        </div>

        <InputComponent
            id="username"
            v-model="username"
            label="Username"
            placeholder="Enter your username"
            validation-type="string"
            :min-length="2"
            error-message="Username must be at least 2 characters long"
            @update:validation="(valid: boolean) => updateValidation('username', valid)" />

        <InputComponent
            id="password"
            v-model="password"
            label="Password"
            type="password"
            placeholder="Enter your password"
            validation-type="string"
            :min-length="4"
            error-message="Password must be at least 4 characters long and match"
            @update:validation="(valid: boolean) => updateValidation('password', valid)" />

        <InputComponent
            id="confirmPassword"
            v-model="confirmPassword"
            label="Confirm Password"
            type="password"
            placeholder="Confirm your password"
            validation-type="string"
            :min-length="4"
            error-message="Password must be at least 4 characters long and match"
            @update:validation="(valid: boolean) => updateValidation('confirmPassword', valid)" />
    </div>
</template>

<script setup lang="ts">
import InputComponent from '@/components/common/InputComponent.vue'

const username = defineModel<string>('username', { default: '' })
const password = defineModel<string>('password', { default: '' })
const confirmPassword = defineModel<string>('confirmPassword', { default: '' })

const emit = defineEmits<{
    'update:validation': [field: string, valid: boolean]
}>()

const updateValidation = (field: string, valid: boolean) => {
    emit('update:validation', field, valid)
}
</script>

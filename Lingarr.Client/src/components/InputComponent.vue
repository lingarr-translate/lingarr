<template>
    <div class="relative">
        <label v-if="label" for="apiInput" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div class="relative">
            <input
                id="apiInput"
                v-model="inputModel"
                :class="[
                    'w-full rounded-md border border-accent bg-transparent px-4 py-2 outline-none transition-colors duration-200',
                    isValid ? 'border-green-500' : 'border-accent',
                    isInvalid ? 'border-red-500' : ''
                ]"
                :placeholder="placeholder"
                @input="validateInput" />
            <div class="absolute inset-y-0 right-0 flex items-center pr-3">
                <span v-if="isValid" class="text-green-500">
                    <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                        <path
                            fill-rule="evenodd"
                            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                            clip-rule="evenodd" />
                    </svg>
                </span>
                <span v-if="isInvalid" class="text-red-500">
                    <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                        <path
                            fill-rule="evenodd"
                            d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                            clip-rule="evenodd" />
                    </svg>
                </span>
            </div>
        </div>
        <p v-if="isInvalid" class="mt-1 text-sm text-red-600">
            {{ error }}
        </p>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, ModelRef } from 'vue'

const {
    label,
    placeholder,
    validationType,
    minLength = 0,
    maxLength = 99,
    errorMessage = ''
} = defineProps<{
    label?: string
    placeholder?: string
    validationType: string
    minLength?: number
    maxLength?: number
    errorMessage?: string
}>()
const inputModel: ModelRef<string> = defineModel({ required: true })

const isValid: Ref<boolean> = ref(false)
const isInvalid: Ref<boolean> = ref(false)
const error: Ref<string> = ref('')

const validateInput = () => {
    const value = inputModel.value

    switch (validationType) {
        case 'string':
            isValid.value = value.length >= minLength && value.length <= maxLength
            error.value = errorMessage.format({ minLength: minLength })
            break
        case 'url':
            const urlPattern = /^(http:\/\/|https:\/\/)[\w\-]+(\.[\w\-]+)*(:\d+)?(\/.*)?$/
            isValid.value = urlPattern.test(value)
            error.value = errorMessage
            break
        default:
            isValid.value = true
            error.value = ''
    }

    isInvalid.value = value !== '' && !isValid.value
}
</script>

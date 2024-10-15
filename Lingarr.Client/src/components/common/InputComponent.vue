<template>
    <div class="relative">
        <label v-if="label" :for="id" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div class="relative">
            <input
                :id="id"
                :value="modelValue"
                :type="type == 'password' ? (showPassword ? 'text' : 'password') : type"
                :class="inputClasses"
                :placeholder="placeholder"
                @input="handleInput" />
            <ValidationIcon
                :is-valid="isValid"
                :is-invalid="isInvalid"
                :class="type == 'password' ? 'pr-10' : 'pr-3'" />
            <button
                v-if="type === 'password'"
                type="button"
                class="absolute inset-y-0 right-0 flex items-center pr-3"
                @click="togglePassword">
                <EyeOnIcon v-if="showPassword" class="h-5 w-5" />
                <EyeOffIcon v-else class="h-5 w-5" />
            </button>
        </div>
        <p v-if="isInvalid" class="mt-1 text-sm text-red-600">
            {{ error }}
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import useValidation from '@/composables/useValidation'
import useDebounce from '@/composables/useDebounce'
import ValidationIcon from '@/components/common/ValidationIcon.vue'
import EyeOnIcon from '@/components/icons/EyeOnIcon.vue'
import EyeOffIcon from '@/components/icons/EyeOffIcon.vue'

const props = defineProps<{
    id?: string
    type?: string
    label?: string
    modelValue: string
    minLength?: number
    maxLength?: number
    placeholder?: string
    errorMessage?: string
    validationType: 'number' | 'string' | 'url'
}>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
    (e: 'validation-status', isValid: boolean): void
}>()

const { isValid, isInvalid, error, validate } = useValidation(props)

const showPassword = ref(false)

const inputClasses = computed(() => [
    'w-full rounded-md border bg-transparent px-4 py-2 outline-none transition-colors duration-200',
    { 'border-green-500': isValid.value },
    { 'border-red-500': isInvalid.value },
    { 'border-accent': !isValid.value && !isInvalid.value },
    { 'pr-10': props.type === 'password' }
])

const debouncedValidate = useDebounce((value: string) => {
    validate(value)
    if (isValid.value) {
        emit('update:modelValue', value)
    }
    emit('validation-status', isValid.value)
}, 500)

const handleInput = (event: Event) => {
    const value = (event.target as HTMLInputElement).value
    debouncedValidate(value)
}

const togglePassword = () => {
    showPassword.value = !showPassword.value
}
</script>

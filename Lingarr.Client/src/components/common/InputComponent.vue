<template>
    <form class="relative">
        <label v-if="label" :for="id" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div v-if="$slots.default" class="mb-1">
            <slot></slot>
        </div>
        <div class="relative">
            <input
                :id="id"
                autocomplete="off"
                :value="modelValue"
                :type="type == 'password' ? (showPassword ? 'text' : 'password') : type"
                :class="inputClasses"
                :placeholder="placeholder"
                @input="handleInput" />
            <ValidationIcon
                :is-valid="isValid"
                :is-invalid="isInvalid"
                :size="size"
                :class="{
                    'pr-10': type === 'password',
                    'pr-3': size === 'md',
                    'pr-1': size === 'sm'
                }" />
            <button
                v-if="type === 'password'"
                type="button"
                class="absolute inset-y-0 right-0 flex cursor-pointer items-center pr-3"
                @click="togglePassword">
                <EyeOnIcon v-if="showPassword" class="h-5 w-5" />
                <EyeOffIcon v-else class="h-5 w-5" />
            </button>
        </div>
        <p v-if="isInvalid" class="mt-1 text-sm text-red-600" v-html="error" />
    </form>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import useValidation from '@/composables/useValidation'
import useDebounce from '@/composables/useDebounce'
import ValidationIcon from '@/components/common/ValidationIcon.vue'
import EyeOnIcon from '@/components/icons/EyeOnIcon.vue'
import EyeOffIcon from '@/components/icons/EyeOffIcon.vue'

const props = withDefaults(
    defineProps<{
        id?: string
        type?: string
        label?: string
        modelValue: string | number | null
        minLength?: number
        maxLength?: number
        placeholder?: string
        errorMessage?: string
        size?: 'sm' | 'md' | 'lg'
        validationType: 'number' | 'string' | 'url' | 'cron'
    }>(),
    {
        size: 'md'
    }
)

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
    (e: 'update:validation', isValid: boolean): void
    (e: 'update:value', value: string): void
}>()
const { isValid, isInvalid, error, validate } = useValidation(props)

const showPassword = ref(false)
const inputClasses = computed(() => [
    '[appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none',
    'w-full rounded-md border bg-transparent outline-hidden transition-colors duration-200 ',
    {
        'px-6 py-3 text-lg': props.size === 'lg',
        'px-4 py-2': props.size === 'md',
        'px-2 py-0.5 leading-3 text-sm': props.size === 'sm'
    },
    { 'border-green-500': isValid.value },
    { 'border-red-500': isInvalid.value },
    { 'border-accent': !isValid.value && !isInvalid.value },
    { 'pr-10': props.type === 'password' }
])

const handleInput = useDebounce((event: Event) => {
    const value = (event.target as HTMLInputElement).value
    validate(value)
    emit('update:validation', isValid.value)
    emit('update:modelValue', value)
    emit('update:value', value)
}, 1000)

const togglePassword = () => {
    showPassword.value = !showPassword.value
}
</script>

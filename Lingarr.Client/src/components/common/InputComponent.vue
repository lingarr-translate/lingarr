<template>
    <div class="relative min-w-0">
        <label v-if="label" :for="inputId" class="mb-2 block text-sm font-medium">
            {{ label }}
        </label>
        <div v-if="$slots.default" :id="`${inputId}-label`" class="mb-2">
            <slot></slot>
        </div>
        <div class="relative">
            <input
                :id="inputId"
                :aria-label="ariaLabel || (!label && !$slots.default ? placeholder : undefined)"
                :aria-labelledby="
                    !label && !ariaLabel && $slots.default ? `${inputId}-label` : undefined
                "
                :aria-invalid="validationType && isInvalid ? true : undefined"
                :aria-describedby="validationType && isInvalid ? `${inputId}-error` : undefined"
                autocomplete="off"
                :value="modelValue"
                :type="type == INPUT_TYPE.PASSWORD ? (showPassword ? 'text' : 'password') : type"
                :class="inputClasses"
                :placeholder="placeholder"
                @input="handleInput" />
            <ValidationIcon
                v-if="validationType"
                class="pointer-events-none"
                :is-valid="isValid"
                :is-invalid="isInvalid"
                :size="size"
                :class="{
                    'pr-10': type === INPUT_TYPE.PASSWORD,
                    'pr-3': size === 'md',
                    'pr-1': size === 'sm'
                }" />
            <button
                v-if="type === INPUT_TYPE.PASSWORD"
                type="button"
                class="absolute inset-y-0 right-0 flex w-11 cursor-pointer items-center justify-center rounded-lg"
                :aria-label="showPassword ? 'Hide password' : 'Show password'"
                v-tooltip="showPassword ? 'Hide password' : 'Show password'"
                @click="togglePassword">
                <EyeOnIcon v-if="showPassword" class="h-5 w-5" />
                <EyeOffIcon v-else class="h-5 w-5" />
            </button>
        </div>
        <p
            v-if="validationType && isInvalid"
            :id="`${inputId}-error`"
            role="alert"
            class="mt-2 text-sm text-[var(--danger)]"
            v-html="error" />
    </div>
</template>

<script setup lang="ts">
import { computed, ref, useId } from 'vue'
import { InputType, INPUT_TYPE, InputValidationType } from '@/ts'
import useValidation from '@/composables/useValidation'
import useDebounce from '@/composables/useDebounce'
import ValidationIcon from '@/components/common/ValidationIcon.vue'
import EyeOnIcon from '@/components/icons/EyeOnIcon.vue'
import EyeOffIcon from '@/components/icons/EyeOffIcon.vue'

const props = withDefaults(
    defineProps<{
        id?: string
        ariaLabel?: string
        type?: InputType
        label?: string
        modelValue: string | number | null
        minLength?: number
        maxLength?: number
        placeholder?: string
        errorMessage?: string
        size?: 'sm' | 'md' | 'lg'
        validationType?: InputValidationType
        debounce?: number
    }>(),
    {
        size: 'md',
        debounce: 1000
    }
)

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
    (e: 'update:validation', isValid: boolean): void
    (e: 'update:value', value: string): void
}>()
const { isValid, isInvalid, error, validate } = useValidation(props)

const generatedId = useId()
const inputId = computed(() => props.id || generatedId)
const showPassword = ref(false)
const inputClasses = computed(() => [
    '[appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none',
    'ui-input w-full',
    {
        'px-6 py-3 text-lg': props.size === 'lg',
        'px-4 py-2': props.size === 'md',
        'px-2 py-2 text-sm': props.size === 'sm'
    },
    { 'border-green-500': props.validationType && isValid.value },
    { 'border-red-500': props.validationType && isInvalid.value },
    { 'pr-10': props.type === 'password' || (props.validationType && props.size !== 'sm') },
    { 'pr-6': props.validationType && props.size === 'sm' }
])

const emitValue = (event: Event) => {
    const value = (event.target as HTMLInputElement).value
    if (props.validationType) {
        validate(value)
        emit('update:validation', isValid.value)
    }
    emit('update:modelValue', value)
    emit('update:value', value)
}

const debouncedValue = useDebounce(emitValue, props.debounce)

const handleInput = (event: Event) => {
    if (props.debounce > 0) {
        debouncedValue(event)
    } else {
        emitValue(event)
    }
}

const togglePassword = () => {
    showPassword.value = !showPassword.value
}
</script>

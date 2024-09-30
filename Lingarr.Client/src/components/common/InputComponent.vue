<template>
    <div class="relative">
        <label v-if="label" :for="label" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div class="relative">
            <input
                :id="label"
                :value="internalValue"
                :type="inputType"
                :class="[
                    'w-full rounded-md border bg-transparent px-4 py-2 outline-none transition-colors duration-200',
                    isValid ? 'border-green-500' : 'border-accent',
                    isInvalid ? 'border-red-500' : ''
                ]"
                :placeholder="placeholder"
                @input="handleInput" />
            <div class="absolute inset-y-0 right-0 flex items-center pr-3">
                <span v-if="isValid" class="text-green-500">
                    <CheckMarkIcon class="h-5 w-5" />
                </span>
                <span v-if="isInvalid" class="text-red-500">
                    <ExclamationIcon class="h-5 w-5" />
                </span>
            </div>
        </div>
        <p v-if="isInvalid" class="mt-1 text-sm text-red-600">
            {{ error }}
        </p>
    </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import useDebounce from '@/composables/useDebounce'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'
import CheckMarkIcon from '@/components/icons/CheckMarkIcon.vue'

const {
    label,
    placeholder,
    validationType,
    minLength = undefined,
    maxLength = undefined,
    inputType = 'string',
    errorMessage = ''
} = defineProps<{
    label?: string
    placeholder?: string
    validationType: string
    inputType?: string
    minLength?: number | undefined
    maxLength?: number | undefined
    errorMessage?: string
}>()

const emit = defineEmits(['validation-status', 'update:modelValue'])
const inputModel = defineModel<string>({ required: true })
const internalValue = ref(inputModel.value)
const error = ref('')
const isValid = ref(false)
const isInvalid = ref(false)

const validateAndUpdate = useDebounce((value: string) => {
    validateInput(value)
    if (isValid.value) {
        emit('update:modelValue', value)
    }
}, 500)

const handleInput = (event: Event) => {
    const value = (event.target as HTMLInputElement).value
    internalValue.value = value
    validateAndUpdate(value)
}

const validateInput = (value: string) => {
    switch (validationType) {
        case 'number':
            isValid.value = !isNaN(Number(value))
            error.value = isValid.value ? (errorMessage ?? 'Please enter a valid number') : ''
            break
        case 'string':
            isValid.value = value.length >= (minLength ?? 0) && value.length <= (maxLength ?? 99)
            error.value = isValid.value
                ? ''
                : (errorMessage ?? `Length must be between ${minLength} and ${maxLength}`)
            break
        case 'url':
            const urlPattern = /^(http:\/\/|https:\/\/)[\w\-]+(\.[\w\-]+)*(:\d+)?(\/.*)?$/
            isValid.value = urlPattern.test(value)
            error.value = isValid.value ? '' : (errorMessage ?? 'Invalid URL')
            break
        default:
            isValid.value = true
            error.value = ''
    }

    isInvalid.value = value !== '' && !isValid.value
    emit('validation-status', isValid.value)
}

watch(inputModel, (newVal) => {
    internalValue.value = newVal
    validateInput(newVal)
})
</script>

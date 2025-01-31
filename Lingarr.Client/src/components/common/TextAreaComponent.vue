<template>
    <div class="relative">
        <label v-if="label" :for="id" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div class="mb-2 flex gap-2">
            <button
                type="button"
                class="bg-accent hover:bg-accent/80 rounded-md px-3 py-1 text-sm"
                @click="insertPlaceholder('{sourceLanguage}')">
                Insert {sourceLanguage}
            </button>
            <button
                type="button"
                class="bg-accent hover:bg-accent/80 rounded-md px-3 py-1 text-sm"
                @click="insertPlaceholder('{targetLanguage}')">
                Insert {targetLanguage}
            </button>
        </div>
        <div class="relative">
            <textarea
                :id="id"
                ref="textareaRef"
                :value="modelValue"
                :class="textareaClasses"
                :placeholder="placeholder"
                :rows="rows"
                @input="handleInput"></textarea>
            <ValidationIcon
                :is-valid="isValid"
                :is-invalid="isInvalid"
                class="absolute top-3 right-3" />
        </div>
        <p v-if="missingPlaceholders.length > 0" class="mt-1 text-sm text-red-600">
            Missing required placeholders: {{ missingPlaceholders.join(' and ') }}
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, nextTick } from 'vue'
import useDebounce from '@/composables/useDebounce'
import ValidationIcon from '@/components/common/ValidationIcon.vue'

const { id, label, modelValue, placeholder, rows } = defineProps<{
    id?: string
    label?: string
    modelValue: string
    placeholder?: string
    rows?: number
}>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
    (e: 'update:validation', isValid: boolean): void
}>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)
const isValid = ref(false)
const isInvalid = ref(false)
const missingPlaceholders = ref<string[]>([])

const textareaClasses = computed(() => [
    'w-full rounded-md border bg-transparent px-4 py-2 outline-hidden transition-colors duration-200',
    { 'border-green-500': isValid.value },
    { 'border-red-500': isInvalid.value },
    { 'border-accent': !isValid.value && !isInvalid.value },
    'pr-10'
])

const validatePlaceholders = (value: string) => {
    const required = ['{sourceLanguage}', '{targetLanguage}']
    const missing = required.filter((placeholder) => !value.includes(placeholder))
    missingPlaceholders.value = missing
    isValid.value = missing.length === 0
    isInvalid.value = missing.length > 0
    return isValid.value
}

const handleInput = useDebounce((event: Event) => {
    const value = (event.target as HTMLTextAreaElement).value
    const isValidInput = validatePlaceholders(value)

    emit('update:validation', isValidInput)
    emit('update:modelValue', value)
}, 1000)

const insertPlaceholder = (placeholder: string) => {
    const textarea = textareaRef.value
    if (!textarea) return

    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const value = textarea.value

    const newValue = value.substring(0, start) + placeholder + value.substring(end)
    emit('update:modelValue', newValue)
    validatePlaceholders(newValue)

    // Force Vue to update the textarea value before setting the cursor position
    nextTick(() => {
        textarea.focus()
        const newPosition = start + placeholder.length
        textarea.setSelectionRange(newPosition, newPosition)
    })
}
</script>

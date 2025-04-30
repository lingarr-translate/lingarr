<template>
    <div class="relative">
        <label v-if="label.length" :for="id" class="mb-1 block text-sm">
            {{ label }}
        </label>
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
            {{ translate('settings.prompt.missingPlaceholders') }}
            {{ missingPlaceholders.join(' and ') }}
        </p>
        <div class="flex flex-wrap gap-3">
            <PlaceholderButton
                v-for="(item, index) in placeholders"
                :key="index"
                :placeholder="item.placeholder"
                :placeholder-text="item.placeholderText"
                :title="item.title"
                :description="item.description"
                @insert="insertPlaceholder" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, nextTick, onMounted } from 'vue'
import useDebounce from '@/composables/useDebounce'
import ValidationIcon from '@/components/common/ValidationIcon.vue'
import PlaceholderButton from '@/components/common/PlaceholderButton.vue'
import { useI18n } from '@/plugins/i18n'
const { translate } = useI18n()

interface Placeholder {
    placeholder: string
    placeholderText: string
    title: string
    description: string
    required?: boolean
}

const props = withDefaults(
    defineProps<{
        id?: string
        label?: string
        modelValue: string
        placeholder?: string
        rows?: number
        placeholders?: Placeholder[]
        requiredPlaceholders?: string[]
    }>(),
    {
        id: `textarea-${Math.random().toString(36).substring(2, 9)}`,
        label: '',
        rows: 4,
        placeholder: '',
        placeholders: () => [],
        requiredPlaceholders: () => []
    }
)

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
    (e: 'update:validation', isValid: boolean): void
}>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)
const isValid = ref(false)
const isInvalid = ref(false)
const missingPlaceholders = ref<string[]>([])

const textareaClasses = computed(() => [
    'w-full resize-y rounded-md border bg-transparent px-4 py-2 outline-hidden transition-colors duration-200',
    { 'border-green-500': isValid.value },
    { 'border-red-500': isInvalid.value },
    { 'border-accent': !isValid.value && !isInvalid.value },
    'pr-10'
])

const validatePlaceholders = (value: string) => {
    const missing = props.requiredPlaceholders.filter((placeholder) => !value.includes(placeholder))
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
    adjustHeight()
}, 300)

const insertPlaceholder = (placeholder: string) => {
    const textarea = textareaRef.value
    if (!textarea) return

    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const value = textarea.value

    const newValue = value.substring(0, start) + placeholder + value.substring(end)
    emit('update:modelValue', newValue)
    validatePlaceholders(newValue)

    nextTick(() => {
        textarea.focus()
        const newPosition = start + placeholder.length
        textarea.setSelectionRange(newPosition, newPosition)
        adjustHeight()
    })
}

const adjustHeight = () => {
    const textarea = textareaRef.value
    if (!textarea) return

    textarea.style.height = 'auto'
    textarea.style.height = `${textarea.scrollHeight}px`
    textarea.style.overflowY = 'hidden'
}

onMounted(() => {
    adjustHeight()
    if (props.modelValue) {
        nextTick(() => {
            adjustHeight()
        })
    }
})
</script>

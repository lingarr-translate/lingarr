<template>
    <label v-if="label" class="mb-1 block text-sm">
        {{ label }}
    </label>
    <div class="flex items-center space-x-3">
        <button
            type="button"
            role="switch"
            :aria-checked="modelValue === 'true'"
            class="relative inline-flex h-7 w-[3.125rem] flex-shrink-0 cursor-pointer items-center rounded-md border border-accent p-1 transition-colors duration-200 ease-in-out"
            @click="toggle">
            <span
                aria-hidden="true"
                class="pointer-events-none inline-block h-5 w-5 transform rounded-md bg-accent shadow ring-0 transition duration-200 ease-in-out"
                :class="modelValue === 'true' ? 'translate-x-5' : 'translate-x-0'"></span>
        </button>
        <span class="text-sm font-medium text-primary-content">
            {{ modelValue === 'true' ? translate('common.enabled') : translate('common.disabled') }}
        </span>
    </div>
</template>

<script setup lang="ts">
const { label, modelValue } = defineProps<{
    label?: string
    modelValue: string
}>()
const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
}>()

const toggle = () => {
    emit('update:modelValue', modelValue === 'true' ? 'false' : 'true')
}
</script>

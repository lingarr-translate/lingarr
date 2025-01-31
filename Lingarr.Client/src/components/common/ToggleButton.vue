<template>
    <label v-if="label" class="mb-1 block text-sm">
        {{ label }}
    </label>
    <div class="flex items-center space-x-3">
        <button
            type="button"
            role="switch"
            :aria-checked="modelValue === 'true'"
            class="border-accent relative inline-flex h-7 w-[3.125rem] shrink-0 cursor-pointer items-center rounded-md border p-1 transition-colors duration-200 ease-in-out"
            @click="toggle">
            <span
                aria-hidden="true"
                class="bg-accent pointer-events-none inline-block h-5 w-5 transform rounded-md ring-0 shadow-sm transition duration-200 ease-in-out"
                :class="modelValue === 'true' ? 'translate-x-5' : 'translate-x-0'"></span>
        </button>
        <span class="text-primary-content text-sm font-medium">
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

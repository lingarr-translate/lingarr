<template>
    <div class="flex items-stretch">
        <button
            v-for="option in options"
            :key="option.value"
            type="button"
            class="cursor-pointer border-b-2 px-4 text-sm font-medium transition-colors duration-200"
            :class="
                option.value === modelValue
                    ? 'border-accent text-primary-content'
                    : 'text-secondary-content/60 hover:text-primary-content border-transparent'
            "
            @click="emit('update:modelValue', option.value)">
            {{ option.label }}
        </button>
    </div>
</template>

<script setup lang="ts">
import { ITabOption } from '@/ts'

defineProps<{
    modelValue: string
    options: ITabOption[]
}>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: string): void
}>()
</script>

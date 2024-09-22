<template>
    <select
        class="block rounded-md border border-accent bg-primary px-2 py-1 text-sm text-primary-content focus:border-accent focus:ring-2 focus:ring-accent"
        :value="modelValue.sortBy"
        @change="sortBy">
        <option
            v-for="(option, index) in options"
            :key="`${option.value}-${index}`"
            :value="option.value">
            {{ option.label }}
        </option>
    </select>
    <button class="flex items-center space-x-2" @click="orderBy">
        <span>
            <svg
                v-if="modelValue.isAscending"
                fill="none"
                viewBox="0 0 24 24"
                stroke-width="2"
                stroke="currentColor"
                class="h-6 w-6">
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 15l7-7 7 7" />
            </svg>
            <svg
                v-else
                fill="none"
                viewBox="0 0 24 24"
                stroke-width="2"
                stroke="currentColor"
                class="h-6 w-6">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
            </svg>
        </span>
    </button>
</template>

<script lang="ts" setup>
import { IOptions, IFilter } from '@/ts'

const emit = defineEmits(['update:modelValue'])
const { modelValue, options } = defineProps<{
    modelValue: IFilter
    options: IOptions[]
}>()

const orderBy = () => {
    emit('update:modelValue', {
        ...modelValue,
        isAscending: !modelValue.isAscending
    })
}

const sortBy = (event: Event) => {
    emit('update:modelValue', {
        ...modelValue,
        sortBy: (event.target as HTMLSelectElement).value
    })
}
</script>

<template>
    <select
        aria-label="Sort by"
        class="ui-input min-w-0 flex-1 px-3 py-2 text-sm sm:w-auto sm:flex-none"
        :value="modelValue.sortBy"
        @change="sortBy">
        <option
            v-for="(option, index) in options"
            :key="`${option.value}-${index}`"
            :value="option.value">
            {{ option.label }}
        </option>
    </select>
    <button
        type="button"
        class="ui-icon-button"
        :aria-label="modelValue.isAscending ? 'Sort descending' : 'Sort ascending'"
        v-tooltip="
            modelValue.isAscending
                ? 'Ascending order. Click to reverse.'
                : 'Descending order. Click to reverse.'
        "
        @click="orderBy">
        <CaretUpIcon v-if="modelValue.isAscending" class="h-6 w-6" />
        <CaretDownIcon v-else class="h-6 w-6" />
    </button>
</template>

<script lang="ts" setup>
import { IOptions, IFilter } from '@/ts'
import CaretUpIcon from '@/components/icons/CaretUpIcon.vue'
import CaretDownIcon from '@/components/icons/CaretDownIcon.vue'

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

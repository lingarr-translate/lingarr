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

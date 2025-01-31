<template>
    <div class="relative w-full md:w-96">
        <input
            :value="modelValue.searchQuery"
            type="text"
            placeholder="Search media..."
            class="border-accent bg-primary text-primary-content placeholder-primary/60 block w-full rounded-md border px-8 py-1 text-sm outline-hidden"
            @input="search" />
        <SearchIcon
            class="text-accent-content absolute top-1/2 left-2 h-4 w-4 -translate-y-1/2 transform" />
        <TimesIcon
            v-if="modelValue.searchQuery"
            class="text-accent-content absolute top-1/2 right-2 h-4 w-4 -translate-y-1/2 transform cursor-pointer"
            @click="clear" />
    </div>
</template>

<script lang="ts" setup>
import { IFilter } from '@/ts'
import SearchIcon from '@/components/icons/SearchIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'

const emit = defineEmits(['update:modelValue'])
const { modelValue } = defineProps<{
    modelValue: IFilter
}>()

const searchQuery = (value: string) => {
    emit('update:modelValue', {
        ...modelValue,
        searchQuery: value
    })
}

const search = (event: Event) => {
    searchQuery((event.target as HTMLInputElement).value)
}

const clear = () => {
    searchQuery('')
}
</script>

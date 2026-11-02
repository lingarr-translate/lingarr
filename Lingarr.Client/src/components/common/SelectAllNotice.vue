<template>
    <div v-if="visible" class="border-accent text-primary-content border-b px-4 py-2 text-sm">
        <template v-if="mode === 'query'">All {{ count }} matching {{ plural }} are selected.</template>
        <template v-else>
            All {{ pageCount }} {{ pageCount === 1 ? singular : plural }} on this page are selected.
            <button type="button" class="cursor-pointer underline" @click="emit('selectAll')">
                Select all {{ totalCount }}
            </button>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { SelectionMode } from '@/utils/selection'

const props = defineProps<{
    mode: SelectionMode
    pageCount: number
    totalCount: number
    count: number
    singular: string
    plural: string
    pageSelected: boolean
}>()

const emit = defineEmits<{ selectAll: [] }>()

const visible = computed(
    () =>
        (props.mode === 'query' && props.count > 0) ||
        (props.pageSelected && props.totalCount > props.pageCount)
)
</script>

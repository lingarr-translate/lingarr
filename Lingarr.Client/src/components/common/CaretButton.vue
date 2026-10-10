<template>
    <button
        type="button"
        class="ui-icon-button"
        :aria-expanded="!isExpanded"
        :aria-label="isExpanded ? 'Expand details' : 'Collapse details'">
        <CaretRightIcon v-if="isExpanded" class="h-4 w-4" />
        <CaretDownIcon v-else class="h-4 w-4" />
    </button>
</template>

<script setup lang="ts">
import CaretDownIcon from '@/components/icons/CaretDownIcon.vue'
import CaretRightIcon from '@/components/icons/CaretRightIcon.vue'

const { isExpanded } = defineProps<{
    isExpanded: boolean
}>()
</script>

<template>
    <button class="p-1">
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

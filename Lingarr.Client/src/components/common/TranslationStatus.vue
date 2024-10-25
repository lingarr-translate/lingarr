<template>
    <span v-if="request && request.status" :title="request.status.toString()">
        {{ formatStatus(request.status) }}
    </span>
    <span v-else-if="item.status" :title="item.status.toString()">
        {{ formatStatus(item.status) }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { IProgressMap, ITranslationRequest, TranslationStatus } from '@/ts'

const { item, progressMap } = defineProps<{
    item: ITranslationRequest
    progressMap: IProgressMap
}>()

const request = computed(() => progressMap.get(item.id))

function formatStatus(status: TranslationStatus): string {
    if (status.toString() == 'InProgress') {
        return 'In Progress'
    }
    return status
}
</script>

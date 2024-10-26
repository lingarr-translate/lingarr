<template>
    <span v-if="item.completedAt" :title="item.completedAt.toString()">
        {{ formatDate(item.completedAt.toString()) }}
    </span>
    <span v-else-if="request && request.completedAt" :title="request.completedAt.toString()">
        {{ formatDate(request.completedAt.toString()) }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { formatDate } from '@/utils/date'
import { IProgressMap, ITranslationRequest } from '@/ts'

const { item, progressMap } = defineProps<{
    item: ITranslationRequest
    progressMap: IProgressMap
}>()

const request = computed(() => progressMap.get(item.id))
</script>

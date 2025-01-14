<template>
    <span :title="formatDateTime(completedAt)">
        {{ formatDate(completedAt) }}
    </span>
</template>

<script setup lang="ts">
import { formatDateTime, formatDate } from '@/utils/date'

defineProps<{
    completedAt: string
}>()
</script>

<template>
    <span>
        {{ formatDate(completedAt) }}
    </span>
</template>

<script setup lang="ts">
import { formatDate } from '@/utils/date'

defineProps<{
    completedAt: string
}>()
</script>

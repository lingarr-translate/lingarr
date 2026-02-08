<template>
    <span :title="translationStatus.toString()">
        {{ translationStatus }}
    </span>
</template>

<script setup lang="ts">
import { TranslationStatus } from '@/ts'

defineProps<{
    translationStatus: TranslationStatus
}>()
</script>

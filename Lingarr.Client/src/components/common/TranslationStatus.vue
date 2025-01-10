<template>
    <span :title="translationStatus.toString()">
        {{ translate(`common.status${translationStatus}`) }}
    </span>
</template>

<script setup lang="ts">
import { useI18n } from '@/plugins/i18n'
import { TranslationStatus } from '@/ts'
const { translate } = useI18n()

defineProps<{
    translationStatus: TranslationStatus
}>()
</script>

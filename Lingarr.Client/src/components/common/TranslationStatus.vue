<template>
    <span :title="translationStatus.toString()">
        {{ translate(`common.status${translationStatus}`) }}
    </span>
</template>

<script setup lang="ts">
import { useI18n } from '@/plugins/i18n'
const { translate } = useI18n()
import { TranslationStatus } from '@/ts'

defineProps<{
    translationStatus: TranslationStatus
}>()
</script>

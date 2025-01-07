<template>
    <span v-if="request && request.status" :title="request.status.toString()">
        {{ translate(`common.status${request.status}`) }}
    </span>
    <span v-else-if="item.status" :title="item.status.toString()">
        {{ translate(`common.status${item.status}`) }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from '@/plugins/i18n'
import { IProgressMap, ITranslationRequest } from '@/ts'
const { translate } = useI18n()

const { item, progressMap } = defineProps<{
    item: ITranslationRequest
    progressMap: IProgressMap
}>()

const request = computed(() => progressMap.get(item.id))
</script>

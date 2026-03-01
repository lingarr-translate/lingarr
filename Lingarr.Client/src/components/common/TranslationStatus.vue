<template>
    <span :title="translationStatus.toString()">
        {{ displayStatus }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { TranslationStatus, TRANSLATION_STATUS } from '@/ts'

const props = defineProps<{
    translationStatus: TranslationStatus
}>()

const displayStatus = computed(() => {
    switch (props.translationStatus) {
        case TRANSLATION_STATUS.INPROGRESS:
            return 'In Progress'
        default:
            return props.translationStatus
    }
})
</script>

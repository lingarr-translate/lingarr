<template>
    <div
        v-if="
            request &&
            request.status !== TRANSLATION_STATUS.COMPLETED &&
            request.status !== TRANSLATION_STATUS.CANCELLED
        "
        class="relative flex-grow select-none"
        :title="`${request.progress}%`">
        <div
            class="h-2 w-full select-none overflow-hidden rounded-full bg-secondary brightness-125">
            <div
                class="h-full select-none rounded-full bg-gradient-to-r from-blue-500 to-purple-500 transition-all duration-1000 ease-out"
                :style="{ width: `${request.progress}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { IProgressMap, TRANSLATION_STATUS } from '@/ts'

const { id, progressMap } = defineProps<{
    id: number
    progressMap: IProgressMap
}>()

const request = computed(() => progressMap.get(id))
</script>

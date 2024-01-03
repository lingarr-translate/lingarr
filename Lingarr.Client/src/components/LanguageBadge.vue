<template>
    <span
        v-if="path"
        class="flex h-6 w-6 items-center justify-center rounded-xl bg-blue-400 px-4 text-xs font-bold uppercase text-blue-100 shadow-sm dark:bg-blue-500">
        {{ path }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface Props {
    subtitle: string
}
const props = defineProps<Props>()

function extractLanguage(filename: string) {
    if (!filename) return
    const pattern = /\.(.{2,3})\.srt$/
    const match = filename.match(pattern)
    return match ? match[1] : null
}

const path = computed(() => extractLanguage(props.subtitle))
</script>

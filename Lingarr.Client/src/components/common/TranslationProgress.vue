<template>
    <div class="relative flex-grow select-none" :title="`${progress}%`">
        <div
            class="h-2 w-full select-none overflow-hidden rounded-full bg-secondary brightness-125">
            <div
                class="h-full select-none rounded-full bg-gradient-to-r from-blue-500 to-purple-500 transition-all duration-1000 ease-out"
                :style="{ width: `${progress}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
defineProps<{
    progress: number
}>()
</script>

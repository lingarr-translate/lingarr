<template>
    <div class="relative grow select-none" :title="`${progress}%`">
        <div
            class="ui-border bg-secondary h-2 w-full overflow-hidden rounded-full border select-none">
            <div
                class="bg-accent h-full rounded-full transition-all duration-1000 ease-out select-none"
                :style="{ width: `${progress}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
defineProps<{
    progress: number
}>()
</script>

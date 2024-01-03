<template>
    <div
        v-if="useInstanceStore().getLoading"
        class="absolute bottom-0 z-10 h-1 w-full overflow-hidden bg-blue-900/50">
        <div class="h-full w-full origin-[0%_50%] animate-loading bg-blue-500/60"></div>
    </div>
</template>

<script setup lang="ts">
import { useInstanceStore } from '@/store/instance'
</script>

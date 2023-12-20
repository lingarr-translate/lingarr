<template>
    <div class="flex h-screen flex-col bg-neutral-900 text-neutral-100">
        <div
            v-if="[STATUS.ERROR, STATUS.SUCCESS].includes(status)"
            class="absolute z-10 mb-2 flex w-full items-center border-l-4 px-3 py-2 text-sm shadow-md"
            :class="statusClass">
            {{ statusMessage }}
        </div>
        <div
            v-if="useInstanceStore().getLoading"
            class="absolute top-0 z-10 h-1 w-full overflow-hidden bg-blue-900/50">
            <div class="h-full w-full origin-[0%_50%] animate-loading bg-blue-500/60"></div>
        </div>
        <main
            class="honeycomb h-full transform overflow-y-auto overflow-x-hidden transition-transform duration-200 ease-in-out">
            <router-view></router-view>
        </main>
    </div>
</template>

<script setup lang="ts">
import { useInstanceStore } from '@/store/instance'
import { computed } from 'vue'
import { STATUS } from '@/ts/instance'

const status = computed(() => useInstanceStore().getStatus)

const statusClass = computed(() => {
    return {
        'bg-green-600/10 text-green-100/80 border-green-700': status.value === STATUS.SUCCESS,
        'bg-red-600/10 text-red-100/80 border-red-700': status.value == STATUS.ERROR
    }
})

const statusMessage = computed(() => {
    switch (status.value) {
        case 'success':
            return 'Subtitles translated successfully!'
        case 'error':
            return 'Error: Subtitle translation failed!'
        default:
            return ''
    }
})
</script>

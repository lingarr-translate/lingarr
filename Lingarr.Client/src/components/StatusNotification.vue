<template>
    <div
        v-if="status !== 'none' && [STATUS.ERROR, STATUS.SUCCESS].includes(status)"
        class="absolute bottom-0 z-10 flex w-full items-center justify-between border-l-4 px-3 py-2 text-sm shadow-md"
        :class="statusClass">
        {{ statusMessage }}
        <CloseIcon class="cursor-pointer" @click="status = STATUS.NONE" />
    </div>
</template>

<script setup lang="ts">
import { WritableComputedRef, computed } from 'vue'
import { useInstanceStore } from '@/store/instance'
import { IStatus, STATUS } from '@/ts/instance'
import CloseIcon from '@/components/icons/CloseIcon.vue'

const instanceStore = useInstanceStore()

const status: WritableComputedRef<IStatus> = computed({
    get: () => instanceStore.getStatus,
    set: (status: IStatus) => {
        instanceStore.setStatus(status)
    }
})

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

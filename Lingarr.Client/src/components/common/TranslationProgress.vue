<template>
    <div class="flex flex-row items-center justify-center space-x-4 p-2">
        <div class="flex-grow select-none">
            <div class="mb-2 flex items-center justify-between text-xs text-gray-300">
                <span class="line-clamp-1">{{ job.fileName }}</span>
                <span v-if="progress == 100">Completed</span>
                <span v-else class="pl-1.5">{{ progress }}%</span>
            </div>
            <div class="h-2 w-full overflow-hidden rounded-full bg-gray-700">
                <div
                    class="h-full rounded-full bg-gradient-to-r from-blue-500 to-purple-500 transition-all duration-1000 ease-out"
                    :style="{ width: `${progress}%` }"></div>
            </div>
        </div>

        <TrashIcon
            class="h-5 w-5 flex-shrink-0 cursor-pointer transition-colors duration-300 hover:text-red-500"
            @click="remove" />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { IRunningJob } from '@/ts'
import { useScheduleStore } from '@/store/schedule'
import { useSignalR } from '@/plugins/signalR'
import TrashIcon from '@/components/icons/TrashIcon.vue'

const scheduleStore = useScheduleStore()

const { job } = defineProps<{
    job: IRunningJob
}>()

const progress = ref(0)
const signalR = useSignalR()

onMounted(async () => {
    signalR.on(
        'ScheduleProgress',
        (data: { jobId: string; progress: number; completed: boolean }) => {
            if (data.jobId === job.jobId) {
                if (!data.completed) {
                    progress.value = data.progress
                } else {
                    signalR.leaveGroup({ group: job.jobId })
                }
            }
        }
    )
})

function remove() {
    signalR.leaveGroup({ group: job.jobId })
    scheduleStore.removeRunningJob(job.jobId)
}
</script>

<template>
    <div class="w-full">
        <div class="bg-tertiary flex flex-wrap items-center justify-end gap-2 p-4">
            <ReloadComponent @toggle:update="scheduleStore.fetchRecurringJobs" />
        </div>

        <div class="w-full px-4">
            <div class="border-accent hidden border-b font-bold md:grid md:grid-cols-12">
                <div class="col-span-5 px-4 py-2">Job Name</div>
                <div class="col-span-2 px-4 py-2">State</div>
                <div class="col-span-2 px-4 py-2">Last Execution</div>
                <div class="col-span-2 px-4 py-2">Next Execution</div>
                <div class="col-span-1 px-4 py-2">Actions</div>
            </div>

            <div
                v-for="job in jobs"
                :key="job.id"
                class="border-accent border-b md:grid md:grid-cols-12">
                <div class="px-4 py-2 md:col-span-5">
                    <span class="font-bold md:hidden">Job Name:&nbsp;</span>
                    {{ job.id }}
                </div>
                <div class="px-4 py-2 md:col-span-2">
                    <span class="font-bold md:hidden">State:&nbsp;</span>
                    {{ job.currentState }}
                </div>
                <div class="px-4 py-2 md:col-span-2">
                    <span class="font-bold md:hidden">Last Execution:&nbsp;</span>
                    <span v-if="job.lastExecution">
                        {{ formatDateTime(job.lastExecution) }}
                    </span>
                </div>
                <div class="px-4 py-2 md:col-span-2">
                    <span class="font-bold md:hidden">Next Execution:&nbsp;</span>
                    <div v-if="job.nextExecution">
                        {{ formatDateTime(job.nextExecution) }}
                    </div>
                </div>
                <div class="px-4 py-2 md:col-span-1">
                    <span class="font-bold md:hidden">Actions:&nbsp;</span>
                    <TriggerJob
                        title="Run"
                        @toggle:trigger="scheduleStore.startJob(job.id)" />
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { Hub } from '@/ts'
import { formatDateTime } from '@/utils/date'
import { useSignalR } from '@/composables/useSignalR'
import { useScheduleStore } from '@/store/schedule'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import TriggerJob from '@/components/common/TriggerJob.vue'

const scheduleStore = useScheduleStore()
const signalR = useSignalR()
const hubConnection = ref<Hub>()
const jobs = computed(() => scheduleStore.getRecurringJobs)

const onJobStateUpdated = (jobId: string, state: string) => {
    const job = jobs.value.find((job) => job.id === jobId)
    if (job) {
        job.currentState = state
    }
}

onMounted(async () => {
    await scheduleStore.fetchRecurringJobs()
    hubConnection.value = await signalR.connect('JobProgress', '/signalr/JobProgress')
    await hubConnection.value.joinGroup({ group: 'JobProgress' })

    hubConnection.value.on('JobStateUpdated', onJobStateUpdated)
})

onUnmounted(async () => {
    hubConnection.value?.off('JobStateUpdated', onJobStateUpdated)
})
</script>

<template>
    <router-view></router-view>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useSignalR } from '@/plugins/signalR'
import { useScheduleStore } from '@/store/schedule'
import { IRunningJob } from '@/ts'

const scheduleStore = useScheduleStore()
const signalR = useSignalR()

onMounted(async () => {
    signalR.on('GroupCompleted', (data: string) => {
        signalR.leaveGroup({ group: data })
        scheduleStore.disconnectJob(data)
    })

    await Promise.all(
        scheduleStore.getRunningJobs.map((job: IRunningJob) =>
            signalR.joinGroup({ group: job.jobId })
        )
    )
})

onUnmounted(() => {
    signalR.off('GroupCompleted', () => {})
})
</script>

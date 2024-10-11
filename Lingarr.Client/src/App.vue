<template>
    <router-view></router-view>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useSignalR } from '@/composables/useSignalR'
import { useScheduleStore } from '@/store/schedule'
import { IRunningJob, ISettings } from '@/ts'
import { useSettingStore } from '@/store/setting'

const scheduleStore = useScheduleStore()
const settingStore = useSettingStore()
const signalR = useSignalR()

onMounted(async () => {
    const scheduleProgress = await signalR.connect('ScheduleProgress', '/signalr/ScheduleProgress')
    scheduleProgress.on('GroupCompleted', (data: string) => {
        scheduleProgress.leaveGroup({ group: data })
        scheduleStore.disconnectJob(data)
    })

    await Promise.all(
        scheduleStore.getRunningJobs.map((job: IRunningJob) =>
            scheduleProgress.joinGroup({ group: job.jobId })
        )
    )

    const settingUpdated = await signalR.connect('SettingUpdated', '/signalr/SettingUpdated')
    await settingUpdated.joinGroup({ group: 'SettingUpdated' })

    settingUpdated.on('Update', (setting: { key: keyof ISettings; value: string }) => {
        settingStore.storeSetting(setting.key, setting.value)
    })
})

onUnmounted(async () => {
    const scheduleProgress = await signalR.connect('ScheduleProgress', '/signalr/ScheduleProgress')
    scheduleProgress.off('GroupCompleted', () => {})
})
</script>

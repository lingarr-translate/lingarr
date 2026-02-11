<template>
    <CardComponent title="Anonymous Telemetry">
        <template #description>
            Help improve Lingarr by sharing anonymous usage statistics. No personal information,
            file names, or subtitle content is collected.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex items-center space-x-2">
                    <span>Share anonymous usage statistics:</span>

                    <ToggleButton v-model="telemetryEnabled">
                        <span class="text-sm font-medium text-primary-content">
                            {{ telemetryEnabled === 'true' ? 'Enabled' : 'Disabled' }}
                        </span>
                    </ToggleButton>
                </div>

                <span class="text-sm">Telemetry that will be sent if enabled:</span>
                <div class="rounded-md bg-primary p-4">
                    <pre class="overflow-x-auto rounded-md p-4 text-xs">{{
                        JSON.stringify(previewData, null, 2)
                    }}</pre>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import services from '@/services'
import SaveNotification from '@/components/common/SaveNotification.vue'

const store = useSettingStore()
const previewData = ref<any>(null)

const telemetryEnabled = computed({
    get: () => store.getSetting(SETTINGS.TELEMETRY_ENABLED) as string,
    set: (value: string) => store.updateSetting(SETTINGS.TELEMETRY_ENABLED, value, true)
})

onMounted(async () => {
    const data = await services.telemetry.preview()
    previewData.value = data
})
</script>

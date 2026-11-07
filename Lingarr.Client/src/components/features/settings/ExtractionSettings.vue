<template>
    <CardComponent title="Subtitle Extraction">
        <template #description>
            Extract embedded text subtitles from video files before translation. When enabled,
            Lingarr downloads ffmpeg into the config folder, a download of about 60 MB that uses
            about 220 MB of disk space. Image subtitles such as PGS and VobSub are not extracted.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Extract embedded subtitles:</span>
                </div>
                <ToggleButton v-model="extractionEnabled">
                    <span class="text-sm font-medium text-primary-content">
                        {{ extractionEnabled == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const extractionEnabled = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SUBTITLE_EXTRACTION_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SUBTITLE_EXTRACTION_ENABLED, newValue, true)
        saveNotification.value?.show()
    }
})
</script>

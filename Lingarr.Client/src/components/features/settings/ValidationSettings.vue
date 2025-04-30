<template>
    <CardComponent :title="translate('settings.validation.title')">
        <template #description>
            {{ translate('settings.validation.description') }}
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.validation.enabled') }}
                    </span>
                </div>
                <ToggleButton v-model="validationEnabled">
                    <span class="text-primary-content text-sm font-medium">
                        {{
                            validationEnabled == 'true'
                                ? translate('common.enabled')
                                : translate('common.disabled')
                        }}
                    </span>
                </ToggleButton>

                <InputComponent
                    v-if="validationEnabled == 'true'"
                    v-model="minDurationMs"
                    validation-type="number"
                    :label="translate('settings.validation.minDurationMs')"
                    @update:validation="(val) => (isValid.minDurationMs = val)">
                    <div class="flex flex-wrap gap-2">
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="minDurationMs = '100'">
                            0.2s
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="minDurationMs = '500'">
                            0.5s
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="minDurationMs = '1000'">
                            1s
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="minDurationMs = '1500'">
                            1.5s
                        </button>
                    </div>
                </InputComponent>

                <InputComponent
                    v-if="validationEnabled == 'true'"
                    v-model="maxDurationSecs"
                    type="number"
                    validation-type="number"
                    :label="translate('settings.validation.maxDurationSecs')"
                    @update:validation="(val) => (isValid.maxDurationSecs = val)" />

                <InputComponent
                    v-if="validationEnabled == 'true'"
                    v-model="minSubtitleLength"
                    type="number"
                    validation-type="number"
                    :label="translate('settings.validation.minSubtitleLength')"
                    @update:validation="(val) => (isValid.minSubtitleLength = val)" />

                <InputComponent
                    v-if="validationEnabled == 'true'"
                    v-model="maxSubtitleLength"
                    type="number"
                    validation-type="number"
                    :label="translate('settings.validation.maxSubtitleLength')"
                    @update:validation="(val) => (isValid.maxSubtitleLength = val)" />

                <InputComponent
                    v-if="validationEnabled == 'true'"
                    v-model="maxFileSizeBytes"
                    type="number"
                    validation-type="number"
                    :label="translate('settings.validation.maxFileSizeBytes')"
                    @update:validation="(val) => (isValid.maxFileSizeBytes = val)">
                    <div class="flex flex-wrap gap-2">
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="maxFileSizeBytes = (512 * 1024).toString()">
                            0.5 KB
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="maxFileSizeBytes = (1024 * 1024).toString()">
                            1 MB
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="maxFileSizeBytes = (1.5 * 1024 * 1024).toString()">
                            1.5 MB
                        </button>
                        <button
                            type="button"
                            class="border-accent hover:bg-accent cursor-pointer rounded border px-2 py-1 text-xs transition-colors hover:text-white"
                            @click="maxFileSizeBytes = (2 * 1024 * 1024).toString()">
                            2 MB
                        </button>
                    </div>
                </InputComponent>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const isValid = reactive({
    maxDurationSecs: true,
    minDurationMs: true,
    minSubtitleLength: true,
    maxSubtitleLength: true,
    maxFileSizeBytes: true
})

const validationEnabled = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SUBTITLE_VALIDATION_ENABLED, newValue, true)
        saveNotification.value?.show()
    }
})

const maxDurationSecs = computed({
    get: () => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_MAXDURATIONSECS) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.SUBTITLE_VALIDATION_MAXDURATIONSECS,
            newValue,
            isValid.maxDurationSecs
        )
        if (isValid.maxDurationSecs) {
            saveNotification.value?.show()
        }
    }
})

const minDurationMs = computed({
    get: () => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_MINDURATIONMS) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.SUBTITLE_VALIDATION_MINDURATIONMS,
            newValue,
            isValid.minDurationMs
        )
        if (isValid.minDurationMs) {
            saveNotification.value?.show()
        }
    }
})

const minSubtitleLength = computed({
    get: () => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_MINSUBTITLELENGTH) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.SUBTITLE_VALIDATION_MINSUBTITLELENGTH,
            newValue,
            isValid.minSubtitleLength
        )
        if (isValid.minSubtitleLength) {
            saveNotification.value?.show()
        }
    }
})

const maxSubtitleLength = computed({
    get: () => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_MAXSUBTITLELENGTH) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.SUBTITLE_VALIDATION_MAXSUBTITLELENGTH,
            newValue,
            isValid.maxSubtitleLength
        )
        if (isValid.maxSubtitleLength) {
            saveNotification.value?.show()
        }
    }
})

const maxFileSizeBytes = computed({
    get: () => settingsStore.getSetting(SETTINGS.SUBTITLE_VALIDATION_MAXFILESIZEBYTES) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(
            SETTINGS.SUBTITLE_VALIDATION_MAXFILESIZEBYTES,
            newValue,
            isValid.maxFileSizeBytes
        )
        if (isValid.maxFileSizeBytes) {
            saveNotification.value?.show()
        }
    }
})
</script>

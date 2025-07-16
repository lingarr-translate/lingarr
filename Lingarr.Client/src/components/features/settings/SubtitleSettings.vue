<template>
    <CardComponent :title="translate('settings.subtitle.title')">
        <template #description>
            {{ translate('settings.subtitle.description') }}
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.subtitle.ignoreCaptions') }}
                    </span>
                    {{ translate('settings.subtitle.ignoreCaptionsDescription') }}
                </div>
                <ToggleButton v-model="ignoreCaptions">
                    <span class="text-primary-content text-sm font-medium">
                        {{
                            ignoreCaptions == 'true'
                                ? translate('common.enabled')
                                : translate('common.disabled')
                        }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.subtitle.fixOverlappingSubtitles') }}
                    </span>
                    {{ translate('settings.subtitle.fixOverlappingSubtitlesDescription') }}
                </div>
                <ToggleButton v-model="fixOverlappingSubtitles">
                    <span class="text-primary-content text-sm font-medium">
                        {{
                            fixOverlappingSubtitles == 'true'
                                ? translate('common.enabled')
                                : translate('common.disabled')
                        }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.subtitle.stripSubtitleFormatting') }}
                    </span>
                    {{ translate('settings.subtitle.stripSubtitleFormattingDescription') }}
                </div>
                <ToggleButton v-model="stripSubtitleFormatting">
                    <span class="text-primary-content text-sm font-medium">
                        {{
                            stripSubtitleFormatting == 'true'
                                ? translate('common.enabled')
                                : translate('common.disabled')
                        }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-y-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">
                            {{ translate('settings.subtitle.useSubtitleTagging') }}
                        </span>
                        {{ translate('settings.subtitle.useSubtitleTaggingDescription') }}
                    </div>
                    <ToggleButton v-model="useSubtitleTagging">
                        <span class="text-primary-content text-sm font-medium">
                            {{
                                useSubtitleTagging == 'true'
                                    ? translate('common.enabled')
                                    : translate('common.disabled')
                            }}
                        </span>
                    </ToggleButton>
                    <InputComponent
                        v-if="useSubtitleTagging == 'true'"
                        v-model="subtitleTag"
                        validation-type="string"
                        :label="translate('settings.subtitle.subtitleTag')"
                        @update:validation="(val) => (isValid.subtitleTag = val)" />
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const isValid = reactive({
    subtitleTag: true
})

const ignoreCaptions = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.IGNORE_CAPTIONS) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.IGNORE_CAPTIONS, newValue, true)
        saveNotification.value?.show()
    }
})

const fixOverlappingSubtitles = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.FIX_OVERLAPPING_SUBTITLES) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.FIX_OVERLAPPING_SUBTITLES, newValue, true)
        saveNotification.value?.show()
    }
})

const stripSubtitleFormatting = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.STRIP_SUBTITLE_FORMATTING) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.STRIP_SUBTITLE_FORMATTING, newValue, true)
        saveNotification.value?.show()
    }
})

const useSubtitleTagging = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.USE_SUBTITLE_TAGGING) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.USE_SUBTITLE_TAGGING, newValue, true)
        saveNotification.value?.show()
    }
})

const subtitleTag = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SUBTITLE_TAG) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SUBTITLE_TAG, newValue, isValid.subtitleTag)
        saveNotification.value?.show()
    }
})
</script>

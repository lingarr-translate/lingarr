<template>
    <CardComponent title="Subtitle">
        <template #description>
            Configure how subtitles are cleaned, and timed to prevent overlaps and remove unwanted
            formatting.
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <SaveNotification ref="saveNotification" />
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        Skip translation if subtitles with caption tags exist:
                    </span>
                    Skip translation if a target subtitle with captions such as Hearing Impaired or
                    Forced already exists.
                </div>
                <ToggleButton v-model="ignoreCaptions">
                    <span class="text-sm font-medium text-primary-content">
                        {{ ignoreCaptions == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Fix overlapping subtitles:</span>
                    Automatically resolves overlapping subtitles by adjusting the timing of the
                    previous, current and next subtitle based on text length.
                </div>
                <ToggleButton v-model="fixOverlappingSubtitles">
                    <span class="text-sm font-medium text-primary-content">
                        {{ fixOverlappingSubtitles == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Strip subtitle formatting:</span>
                    Enable this option to remove all formatting tags (e.g., italics, bold, color,
                    position) from (SRT) subtitles, resulting in plain text subtitles.
                </div>
                <ToggleButton v-model="stripSubtitleFormatting">
                    <span class="text-sm font-medium text-primary-content">
                        {{ stripSubtitleFormatting == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Add translator info</span>
                    Add translator info at the beginning of subtitles.
                </div>
                <ToggleButton v-model="addTranslatorInfo">
                    <span class="text-sm font-medium text-primary-content">
                        {{ addTranslatorInfo == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">Remove language tag</span>
                    Remove language tags (e.g., ".en.") from file names.
                </div>
                <ToggleButton v-model="removeLanguageTag">
                    <span class="text-sm font-medium text-primary-content">
                        {{ removeLanguageTag == 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </ToggleButton>

                <div class="flex flex-col space-y-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">Use subtitle tagging</span>
                        Add a custom tag or text to the subtitle file to identify it as translated
                        by Lingarr.
                    </div>
                    <ToggleButton v-model="useSubtitleTagging">
                        <span class="text-sm font-medium text-primary-content">
                            {{ useSubtitleTagging == 'true' ? 'Enabled' : 'Disabled' }}
                        </span>
                    </ToggleButton>
                    <InputComponent
                        v-if="useSubtitleTagging == 'true'"
                        v-model="subtitleTag"
                        :validation-type="INPUT_VALIDATION_TYPE.STRING"
                        label="Subtitle tag"
                        @update:validation="(val) => (isValid.subtitleTag = val)" />
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { INPUT_VALIDATION_TYPE, SETTINGS } from '@/ts'
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

const addTranslatorInfo = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.ADD_TRANSLATOR_INFO) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.ADD_TRANSLATOR_INFO, newValue, true)
        saveNotification.value?.show()
    }
})

const removeLanguageTag = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.REMOVE_LANGUAGE_TAG) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.REMOVE_LANGUAGE_TAG, newValue, true)
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

<template>
    <CardComponent :title="translate('settings.translate.title')">
        <template #description>
            {{ translate('settings.translate.description') }}
        </template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.translate.sourceAndTargetTitle') }}
                    </span>
                    {{ translate('settings.translate.sourceAndTargetDescription') }}
                </div>
                <SaveNotification ref="saveNotification" />
                <div>
                    <span>{{ translate('settings.translate.selectSourceDescription') }}</span>
                    <LanguageSelect
                        v-model:selected="sourceLanguages"
                        class="w-full"
                        :options="languages" />
                </div>
                <div>
                    <span>{{ translate('settings.translate.selectTargetDescription') }}</span>
                    <LanguageSelect
                        v-model:selected="targetLanguages"
                        class="w-full"
                        :options="selectedTargetLanguages" />
                </div>

                <div class="flex flex-col space-x-2">
                    <span class="font-semibold">
                        {{ translate('settings.translate.fixOverlappingSubtitles') }}
                    </span>
                    {{ translate('settings.translate.fixOverlappingSubtitlesDescription') }}
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
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ILanguage, SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslateStore } from '@/store/translate'

import CardComponent from '@/components/common/CardComponent.vue'
import LanguageSelect from '@/components/features/settings/LanguageSelect.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const translateStore = useTranslateStore()

const languages = computed(() => translateStore.getLanguages)

const sourceLanguages = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.SOURCE_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SOURCE_LANGUAGES, newValue, true, true)
        saveNotification.value?.show()
    }
})

const targetLanguages = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.TARGET_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.TARGET_LANGUAGES, newValue, true, true)
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

const selectedTargetLanguages = computed(() => {
    if (sourceLanguages.value.length === 0) {
        return []
    }

    const allTargets = sourceLanguages.value.flatMap((sourceLanguage) => {
        const sourceTargetSet = languages.value.find((lang) => lang.code === sourceLanguage.code)
        if (!sourceTargetSet) {
            return []
        }
        return sourceTargetSet.targets
    })

    const uniqueTargets = [...new Set(allTargets)]
    return uniqueTargets.map((targetCode) => {
        const languageInfo = languages.value.find((lang) => lang.code === targetCode)
        if (languageInfo) {
            return { ...languageInfo }
        }
    }) as ILanguage[]
})

onMounted(() => {
    translateStore.setLanguages()
})
</script>

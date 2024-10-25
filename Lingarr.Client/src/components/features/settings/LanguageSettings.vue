<template>
    <CardComponent title="Source and target translation">
        <template #description>
            Select a source and target language. Both the source and target are used to optimize the
            translation process.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div>
                <span>Select source languages:</span>
                <LanguageSelect
                    v-model:selected="sourceLanguages"
                    class="w-full"
                    :options="libreLanguages" />
            </div>
            <div>
                <span>Select target languages:</span>
                <LanguageSelect
                    v-model:selected="targetLanguages"
                    class="w-full"
                    :options="selectedTargetLanguages" />
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { ref, WritableComputedRef, ComputedRef, computed } from 'vue'
import { ILanguage, SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

import isoLanguages from '@/statics/iso_languages.json'
import libreLanguages from '@/statics/libre_translate_languages.json'

import CardComponent from '@/components/common/CardComponent.vue'
import LanguageSelect from '@/components/features/settings/LanguageSelect.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const sourceLanguages: WritableComputedRef<ILanguage[]> = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.SOURCE_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SOURCE_LANGUAGES, newValue)
        saveNotification.value?.show()
    }
})
const targetLanguages: WritableComputedRef<ILanguage[]> = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.TARGET_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.TARGET_LANGUAGES, newValue)
        saveNotification.value?.show()
    }
})

const selectedTargetLanguages: ComputedRef<ILanguage[]> = computed(() => {
    if (sourceLanguages.value.length === 0) {
        return []
    }

    const allTargets = sourceLanguages.value.flatMap((sourceLanguage) => {
        const sourceTargetSet = libreLanguages.find((lang) => lang.code === sourceLanguage.code)
        if (!sourceTargetSet) {
            return []
        }
        return sourceTargetSet.targets
    })

    const uniqueTargets = [...new Set(allTargets)]
    return uniqueTargets.map((targetCode) => {
        const languageInfo = isoLanguages.find((lang) => lang.code === targetCode)
        if (languageInfo) {
            return { ...languageInfo }
        }
    }) as ILanguage[]
})
</script>

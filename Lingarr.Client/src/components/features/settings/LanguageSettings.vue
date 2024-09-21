<template>
    <TooltipComponent ref="tooltip" tooltip="Settings saved" alignment="right">
        <CardComponent
            class="relative basis-full md:basis-1/2 2xl:basis-1/3"
            title="Source and target translation">
            <template #description>
                Select a source and then a target language, target translation is used as a
                translate option.
            </template>
            <template #content>
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
                <small>
                    In in future releases the translation process can be automated using these
                    settings.
                </small>
            </template>
        </CardComponent>
    </TooltipComponent>
</template>
<script setup lang="ts">
import { ref, WritableComputedRef, ComputedRef, computed } from 'vue'
import { ILanguage } from '@/ts'
import { useSettingStore } from '@/store/setting'

import isoLanguages from '@/statics/iso_languages.json'
import libreLanguages from '@/statics/libre_translate_languages.json'

import CardComponent from '@/components/common/CardComponent.vue'
import LanguageSelect from '@/components/features/settings/LanguageSelect.vue'
import TooltipComponent from '@/components/common/TooltipComponent.vue'

const tooltip = ref<InstanceType<typeof TooltipComponent> | null>(null)
const settingsStore = useSettingStore()

const sourceLanguages: WritableComputedRef<ILanguage[]> = computed({
    get: (): ILanguage[] => settingsStore.getSetting('source_languages') as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting('source_languages', newValue)
        tooltip.value?.showTooltip()
    }
})
const targetLanguages: WritableComputedRef<ILanguage[]> = computed({
    get: (): ILanguage[] => settingsStore.getSetting('target_languages') as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting('target_languages', newValue)
        tooltip.value?.showTooltip()
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

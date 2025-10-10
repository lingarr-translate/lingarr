<template>
    <div
        v-if="translateStore.hasLanguagesError"
        class="border-accent bg-primary mb-4 rounded-md border p-4">
        <div class="flex items-start">
            <svg
                xmlns="http://www.w3.org/2000/svg"
                class="text-accent mt-1 mr-2 h-5 w-5"
                viewBox="0 0 20 20"
                fill="currentColor">
                <path
                    fill-rule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                    clip-rule="evenodd" />
            </svg>
            <div>
                <p class="text-secondary-content font-medium">
                    {{ translate('settings.translate.languageLoadErrorTitle') }}
                </p>
                <p class="text-secondary-content mt-1 text-sm">
                    {{ translate('settings.translate.languageLoadErrorDescription') }}
                </p>
                <button
                    class="border-accent hover:bg-accent mt-3 cursor-pointer justify-end rounded border px-3 py-2 transition-colors hover:text-white"
                    :disabled="translateStore.isLanguagesLoading"
                    @click="retryLoadLanguages">
                    <span v-if="translateStore.isLanguagesLoading">
                        {{ translate('common.loading') }}
                    </span>
                    <span v-else>{{ translate('common.retry') }}</span>
                </button>
            </div>
        </div>
    </div>

    <template v-if="!translateStore.hasLanguagesError">
        <div class="flex flex-col space-x-2">
            <span class="font-semibold">
                {{ translate('settings.translate.sourceAndTargetTitle') }}
            </span>
            {{ translate('settings.translate.sourceAndTargetDescription') }}
        </div>

        <div v-if="translateStore.isLanguagesLoading" class="py-4">
            <div class="flex items-center space-x-2">
                <LoaderCircleIcon class="h-5 w-5" />
                <span>{{ translate('common.loading') }}</span>
            </div>
        </div>

        <template v-else>
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
        </template>
    </template>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import LanguageSelect from '@/components/features/settings/LanguageSelect.vue'
import { ILanguage, SETTINGS } from '@/ts'
import { useTranslateStore } from '@/store/translate'
import { useSettingStore } from '@/store/setting'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

const settingsStore = useSettingStore()
const translateStore = useTranslateStore()
const languages = computed(() => translateStore.getLanguages)
const emit = defineEmits(['save'])

const sourceLanguages = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.SOURCE_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SOURCE_LANGUAGES, newValue, true, true)
        emit('save')
    }
})

const targetLanguages = computed({
    get: (): ILanguage[] => settingsStore.getSetting(SETTINGS.TARGET_LANGUAGES) as ILanguage[],
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.TARGET_LANGUAGES, newValue, true, true)
        emit('save')
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

function retryLoadLanguages() {
    translateStore.setLanguages()
}

onMounted(() => {
    translateStore.setLanguages()
})
</script>

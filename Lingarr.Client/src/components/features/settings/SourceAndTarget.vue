<template>
    <div
        v-if="translateStore.hasLanguagesError"
        class="mb-4 rounded-md border border-accent bg-primary p-4">
        <div class="flex items-start">
            <svg
                xmlns="http://www.w3.org/2000/svg"
                class="mr-2 mt-1 h-5 w-5 text-accent"
                viewBox="0 0 20 20"
                fill="currentColor">
                <path
                    fill-rule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                    clip-rule="evenodd" />
            </svg>
            <div>
                <p class="font-medium text-secondary-content">Error retrieving languages</p>
                <p class="mt-1 text-sm text-secondary-content">
                    Unknown error occurred while retrieving languages, make sure the api key is set
                    correctly.
                </p>
                <button
                    class="mt-3 cursor-pointer justify-end rounded border border-accent px-3 py-2 transition-colors hover:bg-accent hover:text-white"
                    :disabled="translateStore.isLanguagesLoading"
                    @click="retryLoadLanguages">
                    <span v-if="translateStore.isLanguagesLoading">Loading...</span>
                    <span v-else>Retry</span>
                </button>
            </div>
        </div>
    </div>

    <template v-if="!translateStore.hasLanguagesError">
        <div class="flex flex-col space-x-2">
            <span class="font-semibold">Source and target translation</span>
            Select a source and target language. Both the source and target are used to request
            translations.
        </div>

        <div v-if="translateStore.isLanguagesLoading" class="py-4">
            <div class="flex items-center space-x-2">
                <LoaderCircleIcon class="h-5 w-5" />
                <span>Loading...</span>
            </div>
        </div>

        <template v-else>
            <div>
                <span>Source languages:</span>
                <LanguageSelect
                    v-model:selected="sourceLanguages"
                    class="w-full"
                    :options="languages" />
            </div>
            <div>
                <span>Target languages:</span>
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
    if (!sourceLanguages.value || sourceLanguages.value.length === 0 || !languages.value) {
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

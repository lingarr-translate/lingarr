<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 xl:grid-cols-2 2xl:grid-cols-3">
        <ServicesSettings />
        <LocalAiSettings
            v-if="
                [
                    SERVICE_TYPE.ANTHROPIC,
                    SERVICE_TYPE.DEEPSEEK,
                    SERVICE_TYPE.GEMINI,
                    SERVICE_TYPE.LOCALAI,
                    SERVICE_TYPE.OPENAI
                ].includes(
                    serviceType as 'openai' | 'anthropic' | 'localai' | 'gemini' | 'deepseek'
                )
            " />
        <TranslationSettings v-if="serviceType" :service-type="serviceType" />
        <ValidationSettings />
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { SETTINGS, SERVICE_TYPE } from '@/ts'
import { useSettingStore } from '@/store/setting'
import ServicesSettings from '@/components/features/settings/ServicesSettings.vue'
import TranslationSettings from '@/components/features/settings/TranslationSettings.vue'
import ValidationSettings from '@/components/features/settings/ValidationSettings.vue'
import LocalAiSettings from '@/components/features/settings/LocalAiSettings.vue'

const settingsStore = useSettingStore()

const serviceType = computed(() => settingsStore.getSetting(SETTINGS.SERVICE_TYPE))
</script>

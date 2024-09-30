<template>
    <CardComponent title="Services">
        <template #description>
            Configure the service you would like to use to perform translation
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Select translation service:</span>
                <SelectComponent
                    v-model:selected="serviceType"
                    :options="[
                        {
                            value: 'libretranslate',
                            label: 'LibreTranslate'
                        },
                        {
                            value: 'deepl',
                            label: 'DeepL'
                        }
                    ]" />
                <div v-if="serviceType === 'libretranslate'">
                    <InputComponent
                        v-model="libreTranslateUrl"
                        validation-type="url"
                        label="Address"
                        error-message="Please enter a valid URL (e.g., http://localhost:3000 or https://api.example.com)" />
                </div>
                <div v-if="serviceType === 'deepl'">
                    <InputComponent
                        v-model="deepLApiKey"
                        validation-type="string"
                        :min-length="0"
                        label="API key"
                        error-message="API Key must be greater than {minLength} characters" />
                    <div class="pt-2 text-xs">
                        Please note that DeepL has
                        <a
                            href="https://developers.deepl.com/docs/resources/usage-limits"
                            class="underline"
                            target="_blank">
                            usage limits
                        </a>
                        . A single subtitle file typically contains between 60,000 and 120,000
                        characters. To avoid exceeding these limits, it's recommended to keep
                        automated translation disabled.
                    </div>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { WritableComputedRef, computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()

const serviceType: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.SERVICE_TYPE, newValue)
        saveNotification.value?.show()
    }
})

const libreTranslateUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.LIBRETRANSLATE_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.LIBRETRANSLATE_URL, newValue)
        saveNotification.value?.show()
    }
})
const deepLApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.DEEPL_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.DEEPL_API_KEY, newValue)
        saveNotification.value?.show()
    }
})
</script>

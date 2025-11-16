<template>
    <CardComponent title="API Key">
        <template #description></template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <div class="flex items-center space-x-2">
                    <span>API Authorisation enabled:</span>
                    <ToggleButton v-model="apiKeyEnabled">
                        <span class="text-primary-content text-sm font-medium">
                            {{
                                apiKeyEnabled === 'true'
                                    ? translate('common.enabled')
                                    : translate('common.disabled')
                            }}
                        </span>
                    </ToggleButton>
                </div>
            </div>
            <ApiKeyConfiguration v-if="apiKeyEnabled" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ApiKeyConfiguration from '@/components/onboarding/ApiKeyConfiguration.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)

const isValid = reactive({
    apiKeyEnabled: true
})

const apiKeyEnabled = computed({
    get: () => settingsStore.getSetting(SETTINGS.API_KEY_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(
            SETTINGS.API_KEY_ENABLED,
            newValue.toString(),
            isValid.apiKeyEnabled
        )
        if (isValid.apiKeyEnabled) {
            saveNotification.value?.show()
        }
    }
})
</script>

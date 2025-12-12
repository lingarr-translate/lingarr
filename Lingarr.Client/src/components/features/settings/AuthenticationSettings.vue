<template>
    <CardComponent title="Authentication Settings">
        <template #description></template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <div class="flex items-center space-x-2">
                    <span>Authorisation enabled:</span>
                    <ToggleButton v-model="authEnabled">
                        <span class="text-primary-content text-sm font-medium">
                            {{
                                authEnabled === 'true'
                                    ? "Enabled"
                                    : "Disabled"
                            }}
                        </span>
                    </ToggleButton>
                </div>
            </div>
        </template>
    </CardComponent>
    <CardComponent title="API Key">
        <template #description></template>
        <template #content>
            <ApiKeyConfiguration v-if="authEnabled" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import ApiKeyConfiguration from '@/components/features/settings/authentication/ApiKeyConfiguration.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const settingsStore = useSettingStore()
const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)

const authEnabled = computed({
    get: () => settingsStore.getSetting(SETTINGS.AUTH_ENABLED) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.AUTH_ENABLED, newValue.toString(), true)
        saveNotification.value?.show()
    }
})
</script>

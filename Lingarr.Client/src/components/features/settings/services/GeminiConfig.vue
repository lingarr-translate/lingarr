<template>
    <div class="flex flex-col space-y-2">
        <div>
            {{ translate('settings.services.aiWarningIntro') }}
            <span :class="automationEnabled == 'true' ? 'text-red-500' : 'text-green-500'">
                {{
                    automationEnabled == 'true'
                        ? translate('settings.services.serviceEnabled')
                        : translate('settings.services.serviceDisabled')
                }}
            </span>
        </div>
        <p class="text-xs">
            {{ translate('settings.services.aiCostDescription') }}
        </p>

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            :label="translate('settings.services.apiKey')"
            :min-length="1"
            :error-message="translate('settings.services.apiKeyError')"
            @update:validation="(val) => (apiKeyIsValid = val)" />

        <label class="mb-1 block text-sm">
            {{ translate('settings.services.aiModel') }}
        </label>
        <ComboBox
            ref="selectRef"
            v-model:selected="aiModel"
            :options="options"
            :load-on-open="true"
            :placeholder="translate('settings.services.selectModel')"
            :no-options="errorMessage || translate('settings.services.loadingModels')"
            @fetch-options="loadOptions" />

        <p>
            {{ translate('settings.services.batchSupportAvailable') }}
            <a class="cursor-pointer underline" @click="router.push({ name: 'subtitle-settings' })">
                {{ translate('settings.services.batchSupportLink') }}
            </a>
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import ComboBox from '@/components/common/ComboBox.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import { useRouter } from 'vue-router'
import { useI18n } from '@/plugins/i18n'
import { useModelOptions } from '@/composables/useModelOptions'
const { translate } = useI18n()
const { options, errorMessage, selectRef, loadOptions } = useModelOptions()

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const apiKeyIsValid = ref(false)
const router = useRouter()

const automationEnabled = computed(() => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED))

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.GEMINI_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.GEMINI_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.GEMINI_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.GEMINI_API_KEY, newValue, apiKeyIsValid.value)
        if (apiKeyIsValid.value) {
            emit('save')
        }
    }
})
</script>

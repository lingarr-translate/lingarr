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
            @update:validation="(val) => (isValid.apiKey = val)" />

        <InputComponent
            v-model="version"
            validation-type="string"
            :label="translate('settings.services.versionLabel')"
            :min-length="1"
            :error-message="translate('settings.services.versionError')"
            @update:validation="(val) => (isValid.version = val)" />

        <label class="mb-1 block text-sm">
            {{ translate('settings.services.aiModel') }}
        </label>
        <SelectComponent
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
import { computed, reactive } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import { useI18n } from '@/plugins/i18n'
import { useRouter } from 'vue-router'
import { useModelOptions } from '@/composables/useModelOptions'

const router = useRouter()
const { translate } = useI18n()
// @ts-expect-error - TypeScript doesn't recognize template ref usage
const { options, errorMessage, selectRef, loadOptions } = useModelOptions()

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const isValid = reactive({
    apiKey: false,
    version: false
})

const automationEnabled = computed(
    () => settingsStore.getSetting(SETTINGS.AUTOMATION_ENABLED) as string
)

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_MODEL, newValue, true)
        emit('save')
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_API_KEY, newValue, isValid.apiKey)
        if (isValid.apiKey) {
            emit('save')
        }
    }
})

const version = computed({
    get: () => settingsStore.getSetting(SETTINGS.ANTHROPIC_VERSION) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.ANTHROPIC_VERSION, newValue, isValid.version)
        if (isValid.version) {
            emit('save')
        }
    }
})
</script>

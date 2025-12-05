<template>
    <div class="flex flex-col space-y-2">
        <p class="text-xs">
            {{ translate('settings.services.localAiDescriptionPath') }}
            <span class="bg-primary rounded-md p-1">/v1/chat/completions</span>
            {{ translate('settings.services.localAiDescriptionOr') }}
            <span class="bg-primary my-1 inline-block rounded-md p-1">/api/generate</span>
            {{ translate('settings.services.localAiDescriptionFollow') }}
            <a
                href="https://platform.openai.com/docs/api-reference/chat/create"
                class="underline"
                target="_blank">
                Open AI
            </a>
            {{ translate('settings.services.localAiDescriptionSpecification') }}
        </p>

        <InputComponent
            v-model="address"
            validation-type="url"
            :placeholder="translate('settings.services.localAiPlaceholder')"
            :label="translate('settings.services.serviceAddress')"
            @update:validation="(val) => (isValid.address = val)" />

        <ComboBox
            ref="selectRef"
            v-model:selected="aiModel"
            :options="options"
            :load-on-open="true"
            :placeholder="translate('settings.services.selectModel')"
            :label="translate('settings.services.aiModel')"
            :no-options="errorMessage || translate('settings.services.loadingModels')"
            @fetch-options="loadOptions" />

        <InputComponent
            v-model="apiKey"
            validation-type="string"
            type="password"
            :label="translate('settings.services.apiKey')"
            @update:validation="(val) => (isValid.apiKey = val)" />
        <p class="text-xs">{{ translate('settings.services.localAiNotification') }}</p>

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
import { useRouter } from 'vue-router'
import InputComponent from '@/components/common/InputComponent.vue'
import ComboBox from '@/components/common/ComboBox.vue'
import { useModelOptions } from '@/composables/useModelOptions'

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])
const isValid = reactive({
    address: false,
    model: false,
    apiKey: false
})
const router = useRouter()
const { options, errorMessage, selectRef, loadOptions } = useModelOptions()

const aiModel = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_MODEL) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_MODEL, newValue, isValid.model)
        if (isValid.model) {
            emit('save')
        }
    }
})

const apiKey = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_API_KEY) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_API_KEY, newValue, isValid.apiKey)
        if (isValid.apiKey) {
            emit('save')
        }
    }
})

const address = computed({
    get: () => settingsStore.getSetting(SETTINGS.LOCAL_AI_ENDPOINT) as string,
    set: (newValue: string) => {
        settingsStore.updateSetting(SETTINGS.LOCAL_AI_ENDPOINT, newValue, isValid.address)
        if (isValid.address) {
            emit('save')
        }
    }
})
</script>

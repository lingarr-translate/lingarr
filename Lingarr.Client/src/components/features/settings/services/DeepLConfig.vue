<template>
    <InputComponent
        v-model="deepLApiKey"
        :validation-type="INPUT_VALIDATION_TYPE.STRING"
        :type="INPUT_TYPE.PASSWORD"
        :min-length="1"
        label="API key"
        error-message="API Key must be {minLength} characters"
        @update:validation="(val) => (isValid = val)" />
    <div class="pt-2 text-xs">
        Please note that DeepL has
        <a
            href="https://developers.deepl.com/docs/resources/usage-limits"
            class="underline"
            target="_blank">
            usage limits
        </a>
        and rate limits. A single subtitle file typically contains between 60,000 and 120,000
        characters. To avoid exceeding these limits, it's recommended to keep automated translation
        disabled.
    </div>
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'
import { useSettingStore } from '@/store/setting'
import { INPUT_TYPE, SETTINGS, INPUT_VALIDATION_TYPE } from '@/ts'
import InputComponent from '@/components/common/InputComponent.vue'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const deepLApiKey = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.DEEPL_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.DEEPL_API_KEY, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>

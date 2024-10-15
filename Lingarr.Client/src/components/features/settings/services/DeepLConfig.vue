<template>
    <InputComponent
        v-model="deepLApiKey"
        validation-type="string"
        type="password"
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
        . A single subtitle file typically contains between 60,000 and 120,000 characters. To avoid
        exceeding these limits, it's recommended to keep automated translation disabled.
    </div>
</template>

<script lang="ts" setup>
import { WritableComputedRef, computed } from 'vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'
import InputComponent from '@/components/common/InputComponent.vue'

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const deepLApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.DEEPL_API_KEY) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.DEEPL_API_KEY, newValue)
        emit('save')
    }
})
</script>

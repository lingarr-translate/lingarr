<template>
    <InputComponent
        v-model="libreTranslateUrl"
        validation-type="url"
        :label="translate('settings.services.serviceAddress')"
        :error-message="translate('settings.services.addressUrlError')"
        @update:validation="(val) => (isValid = val)" />
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import InputComponent from '@/components/common/InputComponent.vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const libreTranslateUrl = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.LIBRETRANSLATE_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.LIBRETRANSLATE_URL, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>

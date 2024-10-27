<template>
    <InputComponent
        v-model="libreTranslateUrl"
        validation-type="url"
        label="Address"
        error-message="Please enter a valid URL (e.g., http://localhost:3000 or https://api.example.com)"
        @update:validation="(val) => (isValid = val)" />
</template>
<script setup lang="ts">
import { WritableComputedRef, computed, ref } from 'vue'
import InputComponent from '@/components/common/InputComponent.vue'
import { useSettingStore } from '@/store/setting'
import { SETTINGS } from '@/ts'

const isValid = ref(false)
const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const libreTranslateUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting(SETTINGS.LIBRETRANSLATE_URL) as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting(SETTINGS.LIBRETRANSLATE_URL, newValue, isValid.value)
        if (isValid.value) {
            emit('save')
        }
    }
})
</script>

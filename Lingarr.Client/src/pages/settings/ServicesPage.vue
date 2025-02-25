<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 xl:grid-cols-2 2xl:grid-cols-3">
        <ServicesSettings />
        <TranslationSettings v-if="serviceType" :service-type="serviceType" />
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'
import ServicesSettings from '@/components/features/settings/ServicesSettings.vue'
import TranslationSettings from '@/components/features/settings/TranslationSettings.vue'

const settingsStore = useSettingStore()

const serviceType = computed(() => settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string)
</script>

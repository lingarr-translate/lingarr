<template>
    <CardComponent class="relative basis-full md:basis-1/2 2xl:basis-1/3" title="Media Settings">
        <template #description>Configure the settings for Radarr and Sonarr integrations.</template>
        <template #content>
            <div
                v-if="isSaved"
                class="absolute right-2 top-2 flex items-center rounded-full bg-green-500 px-2 py-1 text-xs font-bold text-white">
                <svg
                    class="mr-1 h-4 w-4"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10" />
                    <path d="m9 12 2 2 4-4" />
                </svg>
                Saved
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Radarr Settings:</span>
                <InputComponent
                    v-model="radarrUrl"
                    validation-type="url"
                    label="Address"
                    error-message="Please enter a valid URL (e.g., http://localhost:3000 or https://api.example.com)" />
                <InputComponent
                    v-model="radarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    label="API Key"
                    error-message="API Key must be {minLength} characters" />
            </div>
            <div class="flex flex-col space-y-2">
                <span class="font-semibold">Sonarr Settings:</span>
                <InputComponent
                    v-model="sonarrUrl"
                    validation-type="url"
                    label="Address"
                    error-message="Please enter a valid Address (e.g., http://localhost:3000 or https://api.example.com)" />
                <InputComponent
                    v-model="sonarrApiKey"
                    :min-length="32"
                    :max-length="32"
                    validation-type="string"
                    label="API Key"
                    error-message="API Key must be {minLength} characters" />
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { WritableComputedRef, computed, ref, Ref } from 'vue'
import CardComponent from '@/components/CardComponent.vue'
import InputComponent from '@/components/InputComponent.vue'

import { useSettingStore } from '@/store/setting'
const settingsStore = useSettingStore()

const isSaved: Ref<boolean> = ref(false)

const radarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting('radarr_api_key') as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting('radarr_api_key', newValue)
    }
})
const sonarrApiKey: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting('sonarr_api_key') as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting('sonarr_api_key', newValue)
    }
})
const radarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting('radarr_url') as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting('radarr_url', newValue)
    }
})
const sonarrUrl: WritableComputedRef<string> = computed({
    get: (): string => settingsStore.getSetting('sonarr_url') as string,
    set: (newValue: string): void => {
        settingsStore.updateSetting('sonarr_url', newValue)
    }
})
</script>

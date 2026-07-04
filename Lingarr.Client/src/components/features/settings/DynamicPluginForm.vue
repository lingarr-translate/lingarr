<template>
    <div class="flex flex-col space-y-3">
        <div
            v-if="manifest.description"
            class="text-secondary-content/80 text-xs"
            v-html="manifest.description" />
        <div
            v-if="manifest.settings.length === 0 && !manifest.description"
            class="text-sm opacity-60">
            This provider has no configurable settings.
        </div>

        <PluginField
            v-for="field in manifest.settings"
            :key="field.key"
            :field="field"
            @save="emit('save')" />
    </div>
</template>

<script setup lang="ts">
import { IPluginManifest } from '@/ts'
import PluginField from '@/components/features/settings/PluginField.vue'

defineProps<{
    manifest: IPluginManifest
}>()

const emit = defineEmits(['save'])
</script>

<template>
    <CardComponent title="Plugins">
        <template #description>
            Third-party translation services loaded from the plugins folder. Credentials are configured on the
            <a class="cursor-pointer underline" @click="router.push({ name: 'services-settings' })">Services</a>
            page.
        </template>
        <template #content>
            <div v-if="isLoading" class="text-sm opacity-60">Loading plugins…</div>
            <div v-else-if="loadError" class="text-sm text-red-500">{{ loadError }}</div>
            <div v-else-if="plugins.length === 0" class="text-sm opacity-60">
                No plugins loaded. Add plugin DLLs to the folder set by
                <CodeSnippet>PLUGINS_PATH</CodeSnippet>
                and restart Lingarr.
            </div>
            <ul v-else class="space-y-2">
                <li
                    v-for="plugin in plugins"
                    :key="plugin.provider"
                    class="rounded-md border border-accent/40 p-3">
                    <span class="font-semibold">{{ plugin.displayName }}</span>
                    <div v-if="plugin.description" class="mt-1 text-xs opacity-60">
                        {{ plugin.description }}
                    </div>
                    <div v-if="plugin.sourceFile" class="mt-1 text-xs opacity-60">
                        <CodeSnippet>{{ plugin.sourceFile }}</CodeSnippet>
                    </div>
                </li>
            </ul>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { IPluginSummary } from '@/ts'
import services from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import CodeSnippet from '@/components/common/CodeSnippet.vue'

const router = useRouter()
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const plugins = ref<IPluginSummary[]>([])

onMounted(async () => {
    try {
        const summaries = await services.plugin.list()
        plugins.value = summaries.filter((plugin) => !plugin.isBuiltIn)
    } catch (error) {
        console.error('Failed to load plugins', error)
        loadError.value = 'Failed to load plugins.'
    } finally {
        isLoading.value = false
    }
})
</script>

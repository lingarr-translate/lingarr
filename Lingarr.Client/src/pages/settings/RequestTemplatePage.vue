<template>
    <div class="w-full">
        <div class="bg-tertiary flex items-stretch gap-3 px-4">
            <div class="flex items-center py-4">
                <ButtonComponent
                    variant="ghost"
                    size="xs"
                    @click="router.push({ name: 'services-settings' })">
                    <ArrowLeft class="ml-1 h-3.5 w-3.5" />
                </ButtonComponent>
            </div>
            <TabComponent
                v-if="tabOptions.length > 1"
                :model-value="service"
                :options="tabOptions"
                @update:model-value="selectService" />
        </div>

        <div
            class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2">
            <SystemPrompt />
            <ContextPrompt />
        </div>

        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 px-4 pb-4">
            <RequestTemplate :service="service" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { IPluginSummary, SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'
import services from '@/services'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import TabComponent from '@/components/common/TabComponent.vue'
import ArrowLeft from '@/components/icons/ArrowLeft.vue'
import RequestTemplate from '@/components/features/settings/template/RequestTemplate.vue'
import SystemPrompt from '@/components/features/settings/SystemPrompt.vue'
import ContextPrompt from '@/components/features/settings/ContextPrompt.vue'

const router = useRouter()
const settingsStore = useSettingStore()

const props = defineProps<{ service: string }>()
const allPlugins = ref<IPluginSummary[]>([])
const pluginsLoaded = ref(false)

const configuredServices = computed<string[]>(() => {
    const raw = (settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string) ?? '[]'
    try {
        return JSON.parse(raw) as string[]
    } catch {
        return []
    }
})

const tabOptions = computed(() => {
    const templated = new Map<string, IPluginSummary>()
    for (const plugin of allPlugins.value) {
        if (plugin.hasRequestTemplate) {
            templated.set(plugin.provider, plugin)
        }
    }

    return configuredServices.value
        .filter((provider) => templated.has(provider))
        .map((provider) => {
            const summary = templated.get(provider)!
            return { value: provider, label: summary.displayName }
        })
})

function selectService(value: string) {
    router.replace({
        name: 'request-template-settings',
        params: { service: value }
    })
}

watch(
    [() => props.service, tabOptions, pluginsLoaded],
    ([current, options, loaded]) => {
        if (!loaded) {
            return
        }
        if (options.length === 0) {
            router.push({ name: 'services-settings' })
            return
        }
        if (!options.some((option) => option.value === current)) {
            selectService(options[0].value)
        }
    },
    { immediate: true }
)

onMounted(async () => {
    try {
        allPlugins.value = await services.plugin.list()
    } catch (error) {
        console.error('Failed to load translation provider list', error)
    } finally {
        pluginsLoaded.value = true
    }
})
</script>

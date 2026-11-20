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
                :model-value="String(serviceId)"
                :options="tabOptions"
                @update:model-value="selectService" />
        </div>

        <div
            class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2">
            <TranslationPrompt :service-id="serviceId" />
            <ProofreadPrompt :service-id="serviceId" />
        </div>

        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 px-4 pb-4">
            <RequestTemplate :service-id="serviceId" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { IPluginSummary } from '@/ts'
import { useTranslationServiceStore } from '@/store/translationService'
import services from '@/services'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import TabComponent from '@/components/common/TabComponent.vue'
import ArrowLeft from '@/components/icons/ArrowLeft.vue'
import RequestTemplate from '@/components/features/settings/template/RequestTemplate.vue'
import TranslationPrompt from '@/components/features/settings/TranslationPrompt.vue'
import ProofreadPrompt from '@/components/features/settings/ProofreadPrompt.vue'

const router = useRouter()
const translationServiceStore = useTranslationServiceStore()

const props = defineProps<{ serviceId: number }>()
const allPlugins = ref<IPluginSummary[]>([])
const pluginsLoaded = ref(false)

const tabOptions = computed(() => {
    const templated = new Set(
        allPlugins.value
            .filter((plugin) => plugin.hasRequestTemplate)
            .map((plugin) => plugin.provider)
    )

    return translationServiceStore.getServices
        .filter((service) => templated.has(service.provider))
        .map((service) => ({ value: String(service.id), label: service.name }))
})

function selectService(value: string) {
    router.replace({
        name: 'request-template-settings',
        params: { serviceId: value }
    })
}

watch(
    [() => props.serviceId, tabOptions, pluginsLoaded, () => translationServiceStore.loaded],
    ([current, options, pluginsReady, servicesReady]) => {
        if (!pluginsReady || !servicesReady) {
            return
        }
        if (options.length === 0) {
            router.push({ name: 'services-settings' })
            return
        }
        if (!options.some((option) => option.value === String(current))) {
            selectService(options[0].value)
        }
    },
    { immediate: true }
)

onMounted(async () => {
    try {
        await translationServiceStore.loadServices()
        allPlugins.value = await services.plugin.list()
    } catch (error) {
        console.error('Failed to load translation services', error)
    } finally {
        pluginsLoaded.value = true
    }
})
</script>

<template>
    <CardComponent title="Services">
        <template #description>
            Configure the translation service for subtitle localization. Add fallback services to be
            tried in order when the primary service fails.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <div class="space-y-2">
                <span class="font-semibold">Translation services</span>
                <ol class="space-y-2">
                    <li
                        v-for="(serviceName, index) in services"
                        :key="`${serviceName}-${index}`"
                        class="flex items-center gap-3 rounded-md border p-3"
                        :class="
                            index === configuringIndex
                                ? 'border-accent bg-accent/10'
                                : 'border-accent/30'
                        ">
                        <span
                            class="bg-accent/20 text-accent-content shrink-0 rounded px-2 py-0.5 text-xs font-semibold tracking-wider uppercase"
                            aria-hidden="true">
                            {{ index === 0 ? 'Primary' : `Fallback ${index}` }}
                        </span>
                        <div class="min-w-0 flex-1">
                            <SelectComponent
                                :selected="serviceName"
                                :options="optionsForRow(index)"
                                size="sm"
                                @update:selected="(value: string) => createRow(index, value)" />
                        </div>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="index === configuringIndex"
                            :aria-pressed="index === configuringIndex"
                            title="Configure credentials"
                            aria-label="Configure credentials"
                            @click="configuringIndex = index">
                            <SettingIcon class="h-4 w-4" />
                        </button>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="index === 0"
                            title="Move up"
                            aria-label="Move up"
                            @click="moveRow(index, -1)">
                            <CaretUpIcon class="h-4 w-4" />
                        </button>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="index === services.length - 1"
                            title="Move down"
                            aria-label="Move down"
                            @click="moveRow(index, 1)">
                            <CaretDownIcon class="h-4 w-4" />
                        </button>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="services.length <= 1"
                            title="Remove service"
                            aria-label="Remove service"
                            @click="removeRow(index)">
                            <TrashIcon class="h-4 w-4" />
                        </button>
                    </li>
                    <li v-if="services.length < serviceOptions.length && serviceOptions.length > 0">
                        <ButtonComponent variant="ghost" size="xs" @click="addRow">
                            <PlusIcon class="mr-1 h-3 w-3" />
                            Add fallback service
                        </ButtonComponent>
                    </li>
                </ol>
            </div>

            <div v-if="configuringManifest || manifestError" class="mt-4 space-y-2">
                <div class="text-sm">
                    <span class="text-secondary-content/60">Configuring credentials for</span>
                    <span class="ml-1 font-semibold">{{ configuringLabel }}</span>
                </div>
                <DynamicPluginForm
                    v-if="configuringManifest"
                    :manifest="configuringManifest"
                    @save="saveNotification?.show()" />
                <p v-else-if="manifestError" class="text-sm text-red-500">{{ manifestError }}</p>
            </div>

            <div v-if="configuringManifest?.hasRequestTemplate" class="mt-6">
                <div class="flex flex-col gap-4">
                    <div class="flex flex-col space-x-2">
                        <span class="font-semibold">Customize request template and prompts</span>
                        Adjust the AI request body, system prompt and context for translations.
                    </div>
                    <ButtonComponent
                        variant="primary"
                        size="md"
                        @click="
                            router.push({
                                name: 'request-template-settings',
                                params: { service: services[configuringIndex] }
                            })
                        ">
                        Open Request Settings
                        <ArrowRight class="mt-1 ml-1 h-4 w-4" />
                    </ButtonComponent>
                </div>
            </div>

            <SourceAndTarget @save="saveNotification?.show()" />
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useSettingStore } from '@/store/setting'
import { IPluginManifest, IPluginSummary, SETTINGS, SERVICE_TYPE } from '@/ts'
import servicesApi from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import SaveNotification from '@/components/common/SaveNotification.vue'
import DynamicPluginForm from '@/components/features/settings/DynamicPluginForm.vue'
import SourceAndTarget from '@/components/features/settings/SourceAndTarget.vue'
import ArrowRight from '@/components/icons/ArrowRight.vue'
import CaretUpIcon from '@/components/icons/CaretUpIcon.vue'
import CaretDownIcon from '@/components/icons/CaretDownIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'

const saveNotification = ref<InstanceType<typeof SaveNotification> | null>(null)
const settingsStore = useSettingStore()
const router = useRouter()

const serviceOptions = ref<{ value: string; label: string }[]>([])

function parseServices(raw: unknown): string[] {
    const list = JSON.parse((raw as string) ?? '[]') as string[]
    return list.length > 0 ? list : [SERVICE_TYPE.LIBRETRANSLATE]
}

const services = ref<string[]>(parseServices(settingsStore.getSetting(SETTINGS.SERVICE_TYPE)))
const configuringIndex = ref(0)
const configuringManifest = ref<IPluginManifest | null>(null)
const manifestError = ref<string | null>(null)

async function loadManifest(provider: string) {
    try {
        const manifest = await servicesApi.plugin.getManifest(provider)
        await settingsStore.setPluginSettings(manifest.settings)

        configuringManifest.value = manifest
        manifestError.value = null
    } catch (error) {
        console.error('Failed to load manifest', error)
        configuringManifest.value = null
        manifestError.value = `No manifest available for ${provider}.`
    }
}

function optionsForRow(index: number) {
    const usedElsewhere = new Set(
        services.value.filter((_, currentIndex) => currentIndex !== index)
    )
    return serviceOptions.value.filter((option) => !usedElsewhere.has(option.value))
}

async function save(next: string[]) {
    services.value = next
    await settingsStore.updateSetting(SETTINGS.SERVICE_TYPE, JSON.stringify(next), true)
    saveNotification.value?.show()
}

function createRow(index: number, value: string) {
    const next = services.value.map((service, currentIndex) =>
        currentIndex === index ? value : service
    )
    save(next)
}

function addRow() {
    const unused = serviceOptions.value.find((option) => !services.value.includes(option.value))
    if (!unused) {
        return
    }
    save([...services.value, unused.value])
}

function removeRow(index: number) {
    if (services.value.length <= 1) {
        return
    }
    save(services.value.filter((_, currentIndex) => currentIndex !== index))
    configuringIndex.value = Math.min(configuringIndex.value, services.value.length - 1)
}

function moveRow(index: number, delta: number) {
    const target = index + delta
    if (target < 0 || target >= services.value.length) {
        return
    }
    const next = [...services.value]
    const moved = next[index]
    next[index] = next[target]
    next[target] = moved
    if (configuringIndex.value === index) {
        configuringIndex.value = target
    } else if (configuringIndex.value === target) {
        configuringIndex.value = index
    }
    save(next)
}

const configuringLabel = computed(() => {
    const value = services.value[configuringIndex.value]
    return serviceOptions.value.find((option) => option.value === value)?.label ?? value
})

watch(
    () => settingsStore.getSetting(SETTINGS.SERVICE_TYPE),
    (raw) => {
        services.value = parseServices(raw)
    }
)

watch(
    () => services.value[configuringIndex.value],
    loadManifest,
    { immediate: true }
)

onMounted(async () => {
    try {
        const summaries: IPluginSummary[] = await servicesApi.plugin.list()
        serviceOptions.value = summaries
            .map((summary) => ({ value: summary.provider, label: summary.displayName }))
            .sort((a, b) => a.label.localeCompare(b.label))
    } catch (error) {
        console.error('Failed to load translation provider list', error)
    }
})
</script>

<template>
    <CardComponent title="Services">
        <template #description>
            Configure the translation service for subtitle localization. Add fallback services to be
            tried in order when the primary service fails. A service that has settings can be added
            more than once, for example to use different models or endpoints.
        </template>
        <template #content>
            <SaveNotification ref="saveNotification" />

            <div class="space-y-2">
                <span class="font-semibold">Translation services</span>
                <ol class="space-y-2">
                    <li
                        v-for="(service, index) in translationServiceStore.getServices"
                        :key="service.id"
                        class="flex items-center gap-3 rounded-md border p-3"
                        :class="
                            service.id === configuringId
                                ? 'border-accent bg-accent/10'
                                : 'border-accent/30'
                        ">
                        <span
                            class="bg-accent/20 text-accent-content shrink-0 rounded px-2 py-0.5 text-xs font-semibold tracking-wider uppercase"
                            aria-hidden="true">
                            {{ index === 0 ? 'Primary' : `Fallback ${index}` }}
                        </span>
                        <div class="min-w-0 flex-1">
                            <div class="truncate font-medium">{{ service.name }}</div>
                            <div class="text-secondary-content/60 truncate text-xs">
                                {{ providerLabel(service.provider) }}
                            </div>
                        </div>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="service.id === configuringId"
                            :aria-pressed="service.id === configuringId"
                            title="Configure service"
                            aria-label="Configure service"
                            @click="selectService(service.id)">
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
                            :disabled="index === translationServiceStore.getServices.length - 1"
                            title="Move down"
                            aria-label="Move down"
                            @click="moveRow(index, 1)">
                            <CaretDownIcon class="h-4 w-4" />
                        </button>
                        <button
                            type="button"
                            class="text-primary-content hover:text-primary-content/50 focus-visible:ring-accent cursor-pointer rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-30"
                            :disabled="translationServiceStore.getServices.length <= 1"
                            title="Remove service"
                            aria-label="Remove service"
                            @click="removeRow(service.id)">
                            <TrashIcon class="h-4 w-4" />
                        </button>
                    </li>
                    <li v-if="providerOptions.length > 0">
                        <div v-if="isAdding" class="max-w-xs">
                            <SelectComponent
                                selected=""
                                :options="providerOptions"
                                placeholder="Select a service..."
                                size="sm"
                                @update:selected="addService" />
                        </div>
                        <ButtonComponent v-else variant="ghost" size="xs" @click="isAdding = true">
                            <PlusIcon class="mr-1 h-3 w-3" />
                            Add service
                        </ButtonComponent>
                    </li>
                </ol>
                <p v-if="errorMessage" class="text-sm text-red-500">{{ errorMessage }}</p>
            </div>

            <div v-if="configuringService" class="mt-4 space-y-3">
                <div class="text-sm">
                    <span class="text-secondary-content/60">Configuring</span>
                    <span class="ml-1 font-semibold">
                        {{ providerLabel(configuringService.provider) }}
                    </span>
                </div>
                <InputComponent
                    id="service-name"
                    v-model="serviceName"
                    label="Name"
                    :validation-type="INPUT_VALIDATION_TYPE.STRING"
                    :min-length="1"
                    @update:validation="(value: boolean) => (isNameValid = value)" />
                <DynamicPluginForm
                    v-if="
                        configuringManifest && translationServiceStore.settings[configuringService.id]
                    "
                    :key="configuringService.id"
                    :service-id="configuringService.id"
                    :manifest="configuringManifest"
                    @save="saveNotification?.show()" />
                <p v-else-if="pluginsLoaded && !configuringManifest" class="text-sm text-red-500">
                    No manifest available for {{ configuringService.provider }}.
                </p>
            </div>

            <div v-if="configuringManifest?.hasRequestTemplate && configuringService" class="mt-6">
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
                                params: { serviceId: configuringService?.id }
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
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useTranslationServiceStore } from '@/store/translationService'
import { INPUT_VALIDATION_TYPE, IPluginManifest } from '@/ts'
import servicesApi from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
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
const translationServiceStore = useTranslationServiceStore()
const router = useRouter()

const plugins = ref<IPluginManifest[]>([])
const pluginsLoaded = ref(false)
const configuringId = ref<number | null>(null)
const isAdding = ref(false)
const isNameValid = ref(true)
const errorMessage = ref<string | null>(null)

const configuringService = computed(() =>
    configuringId.value === null
        ? undefined
        : translationServiceStore.getService(configuringId.value)
)

const configuringManifest = computed<IPluginManifest | undefined>(() => {
    const provider = configuringService.value?.provider
    return plugins.value.find((plugin) => plugin.provider === provider)
})

const providerOptions = computed(() => {
    const usedProviders = new Set(
        translationServiceStore.getServices.map((service) => service.provider)
    )
    return plugins.value
        .filter((plugin) => plugin.settings.length > 0 || !usedProviders.has(plugin.provider))
        .map((plugin) => ({ value: plugin.provider, label: plugin.displayName }))
        .sort((a, b) => a.label.localeCompare(b.label))
})

const serviceName = computed<string>({
    get: (): string => configuringService.value?.name ?? '',
    set: async (newValue: string): Promise<void> => {
        if (!configuringService.value || !isNameValid.value) {
            return
        }
        const id = configuringService.value.id
        await runAction(() => translationServiceStore.renameService(id, newValue))
    }
})

function providerLabel(provider: string): string {
    return plugins.value.find((plugin) => plugin.provider === provider)?.displayName ?? provider
}

async function runAction(action: () => Promise<void>): Promise<void> {
    try {
        errorMessage.value = null
        await action()
        saveNotification.value?.show()
    } catch (error) {
        const response = error as { data?: { message?: string } } | undefined
        errorMessage.value = response?.data?.message ?? 'The request failed. Please try again.'
    }
}

async function selectService(id: number): Promise<void> {
    configuringId.value = id
    try {
        await translationServiceStore.loadSettings(id)
    } catch (error) {
        console.error('Failed to load service settings', error)
    }
}

async function addService(provider: string): Promise<void> {
    isAdding.value = false
    await runAction(async () => {
        const created = await translationServiceStore.createService(provider)
        await selectService(created.id)
    })
}

async function removeRow(id: number): Promise<void> {
    await runAction(async () => {
        await translationServiceStore.removeService(id)
        const primary = translationServiceStore.getPrimary
        if (configuringId.value === id && primary) {
            await selectService(primary.id)
        }
    })
}

async function moveRow(index: number, delta: number): Promise<void> {
    const ids = translationServiceStore.getServices.map((service) => service.id)
    const target = index + delta
    if (target < 0 || target >= ids.length) {
        return
    }
    const moved = ids[index]
    ids[index] = ids[target]
    ids[target] = moved
    await runAction(() => translationServiceStore.reorderServices(ids))
}

onMounted(async () => {
    try {
        await translationServiceStore.loadServices()
        const primary = translationServiceStore.getPrimary
        if (primary) {
            await selectService(primary.id)
        }
    } catch (error) {
        console.error('Failed to load translation services', error)
    }
    try {
        plugins.value = await servicesApi.plugin.list()
    } catch (error) {
        console.error('Failed to load translation provider list', error)
    } finally {
        pluginsLoaded.value = true
    }
})
</script>

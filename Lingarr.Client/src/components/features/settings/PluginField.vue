<template>
    <div>
        <template v-if="field.type === PLUGIN_SETTING_TYPE.REMOTE_DROPDOWN">
            <label class="mb-1 block text-sm font-semibold" :for="field.key">
                {{ field.label }}
                <span v-if="field.required" class="text-red-500" aria-hidden="true">*</span>
            </label>
            <SelectComponent
                ref="remoteSelect"
                :options="remoteOptions"
                :selected="fieldValue"
                :load-on-open="true"
                placeholder="Select option..."
                :no-options="remoteError ?? 'Loading options...'"
                @update:selected="(value: string) => (fieldValue = value)"
                @fetch-options="loadRemoteOptions" />
        </template>
        <template v-else-if="field.type === PLUGIN_SETTING_TYPE.TOGGLE">
            <label class="mb-1 block text-sm font-semibold">
                {{ field.label }}
            </label>
            <ToggleButton v-model="fieldValue">
                <span class="text-sm font-medium text-primary-content">
                    {{ fieldValue === 'true' ? 'Enabled' : 'Disabled' }}
                </span>
            </ToggleButton>
        </template>
        <InputComponent
            v-else
            :id="field.key"
            v-model="fieldValue"
            :type="field.type === PLUGIN_SETTING_TYPE.SECRET ? INPUT_TYPE.PASSWORD : INPUT_TYPE.TEXT"
            :label="field.label"
            :placeholder="field.default ?? ''"
            :validation-type="validationType"
            :min-length="field.minLength ?? undefined"
            :error-message="field.validationErrorMessage ?? undefined"
            @update:validation="(value: boolean) => (isValid = value)" />
        <div v-if="field.description" class="mt-1 text-xs opacity-60">
            {{ field.description }}
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import {
    INPUT_TYPE,
    INPUT_VALIDATION_TYPE,
    InputValidationType,
    IPluginSettingField,
    LabelValue,
    PLUGIN_SETTING_TYPE,
    SelectComponentExpose
} from '@/ts'
import services from '@/services'
import { useTranslationServiceStore } from '@/store/translationService'
import InputComponent from '@/components/common/InputComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const props = defineProps<{
    serviceId: number
    field: IPluginSettingField
}>()

const emit = defineEmits(['save'])

const translationServiceStore = useTranslationServiceStore()

const isValid = ref<boolean>(
    props.field.minLength == null && props.field.type !== PLUGIN_SETTING_TYPE.URL
)

const remoteOptions = ref<LabelValue[]>([])
const remoteError = ref<string | null>(null)
const remoteSelect = ref<SelectComponentExpose | null>(null)

const validationType = computed<InputValidationType | undefined>(() => {
    if (props.field.type === PLUGIN_SETTING_TYPE.URL) {
        return INPUT_VALIDATION_TYPE.URL
    }
    if (props.field.minLength != null) {
        return INPUT_VALIDATION_TYPE.STRING
    }
    return undefined
})

const fieldValue = computed<string>({
    get: (): string => {
        const stored = translationServiceStore.getSetting(props.serviceId, props.field.key)
        if (typeof stored === 'string') {
            return stored
        }
        if (props.field.type === PLUGIN_SETTING_TYPE.SECRET) {
            return ''
        }
        return props.field.default ?? ''
    },
    set: (newValue: string): void => {
        translationServiceStore.updateSetting(
            props.serviceId,
            props.field.key,
            newValue,
            isValid.value
        )
        if (isValid.value) {
            emit('save')
        }
    }
})

async function loadRemoteOptions(): Promise<void> {
    if (!props.field.optionsEndpoint) {
        remoteError.value = 'No options endpoint configured.'
        remoteSelect.value?.setLoadingState(false)
        return
    }

    try {
        remoteError.value = null
        const response = await services.plugin.getOptions(props.field.optionsEndpoint, props.serviceId)
        remoteOptions.value = response.options ?? []
        if (remoteOptions.value.length === 0) {
            remoteError.value = response.message ?? 'No options returned.'
        }
    } catch (error) {
        console.error('Failed to load options:', error)
        remoteError.value = 'Error loading options. Please try again.'
    } finally {
        remoteSelect.value?.setLoadingState(false)
    }
}
</script>

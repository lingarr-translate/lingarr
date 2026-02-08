<template>
    <CardComponent title="Custom Parameters">
        <template #description>
            Define custom parameters (e.g., temperature, top_p) to include with requests to your custom endpoint. Only modify these if you understand what you're doing, errors can be found in the Docker logs.
        </template>
        <template #content>
            <div>
                <div class="mb-2 flex w-full justify-between">
                    <div class="flex-1">Key</div>
                    <div class="flex-1">Value</div>
                    <div class="w-5"></div>
                </div>
                <div
                    v-for="(param, index) in parameters"
                    :key="index"
                    class="mb-3 flex items-center gap-2">
                    <InputComponent
                        :model-value="param.key"
                        validation-type="string"
                        placeholder="temperature"
                        class="flex-1"
                        @update:model-value="updateKey(index, $event)" />
                    <InputComponent
                        :model-value="param.value"
                        validation-type="string"
                        placeholder="0.3"
                        class="flex-1"
                        @update:model-value="updateValue(index, $event)" />
                    <TrashIcon
                        class="flex h-5 w-5 cursor-pointer"
                        title="Remove Parameter"
                        @click="removeParameter(index)" />
                </div>

                <div class="flex justify-end">
                    <ButtonComponent size="sm"
                                     variant="accent"
                                     @click="addParameter">
                        Add Parameter
                    </ButtonComponent>
                </div>
            </div>
        </template>
    </CardComponent>
</template>
<script setup lang="ts">
import { computed, WritableComputedRef } from 'vue'
import { useSettingStore } from '@/store/setting'
import { ICustomAiParams, SETTINGS } from '@/ts'
import InputComponent from '@/components/common/InputComponent.vue'
import CardComponent from '@/components/common/CardComponent.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'

const settingsStore = useSettingStore()
const emit = defineEmits(['save'])

const parameters: WritableComputedRef<ICustomAiParams[]> = computed({
    get: () => settingsStore.getSetting(SETTINGS.CUSTOM_AI_PARAMETERS) as ICustomAiParams[],
    set: (value) => {
        settingsStore.updateSetting(SETTINGS.CUSTOM_AI_PARAMETERS, value, true, true)
        emit('save')
    }
})

function addParameter() {
    parameters.value = [...parameters.value, { key: '', value: '' }]
}

function removeParameter(jndex: number) {
    parameters.value = parameters.value.filter((_, index) => index !== jndex)
}

function updateKey(index: number, value: string) {
    const updated = [...parameters.value]
    updated[index] = { ...updated[index], key: value }
    parameters.value = updated
}

function updateValue(index: number, value: string) {
    const updated = [...parameters.value]
    updated[index] = { ...updated[index], value: value }
    parameters.value = updated
}
</script>

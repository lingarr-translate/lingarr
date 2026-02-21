<template>
    <div class="w-full">
        <div class="flex items-center bg-tertiary p-4">
            <ButtonComponent
                variant="ghost"
                size="xs"
                @click="router.push({ name: 'services-settings' })">
                <ArrowLeft class="ml-1 h-3.5 w-3.5" />
            </ButtonComponent>
        </div>

        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 md:grid-cols-2 xl:grid-cols-2">
            <SystemPrompt/>
            <ContextPrompt/>
        </div>

        <div class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 px-4 pb-4">
            <RequestTemplate />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { SETTINGS, SERVICE_TYPE } from '@/ts'
import { useSettingStore } from '@/store/setting'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import ArrowLeft from '@/components/icons/ArrowLeft.vue'
import RequestTemplate from '@/components/features/settings/template/RequestTemplate.vue'
import SystemPrompt from '@/components/features/settings/SystemPrompt.vue'
import ContextPrompt from '@/components/features/settings/ContextPrompt.vue'

const router = useRouter()
const settingsStore = useSettingStore()

const AI_TEMPLATE_SERVICES = new Set([
    SERVICE_TYPE.OPENAI,
    SERVICE_TYPE.ANTHROPIC,
    SERVICE_TYPE.LOCALAI,
    SERVICE_TYPE.GEMINI,
    SERVICE_TYPE.DEEPSEEK,
])

const serviceType = computed(() => (settingsStore.getSetting(SETTINGS.SERVICE_TYPE) as string) ?? '')

watch(
    serviceType,
    (type) => {
        if (type && !AI_TEMPLATE_SERVICES.has(type)) {
            router.push({ name: 'services-settings' })
        }
    },
    { immediate: true }
)
</script>

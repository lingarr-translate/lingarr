<template>
    <div class="relative select-none items-center transition duration-300 ease-in-out">
        <!-- Context -->
        <TooltipComponent ref="tooltip" alignment="left">
            <div ref="clickOutside" @click="toggle">
                <slot></slot>
            </div>
        </TooltipComponent>
        <!-- Menu -->
        <div
            v-show="isOpen"
            ref="excludeClickOutside"
            class="absolute right-0 top-8 z-10 w-56 rounded-md border border-accent bg-primary bg-clip-border shadow-lg">
            <div class="px-3 py-1" role="menu" aria-orientation="vertical">
                <span class="text-xs" role="menuitem">Translate to ...</span>
                <div
                    v-for="language in languages"
                    :key="language.code"
                    class="mb-1 flex text-sm"
                    role="menuitem"
                    @click="selectOption(language)">
                    <span class="h-full w-full cursor-pointer py-2 hover:brightness-150">
                        {{ language.name }}
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, computed, ComputedRef } from 'vue'
import { useRouter } from 'vue-router'
import { IEpisode, ILanguage, IMovie, ISubtitle, MediaType, SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslateStore } from '@/store/translate'
import useClickOutside from '@/composables/useClickOutside'
import TooltipComponent from '@/components/common/TooltipComponent.vue'

const router = useRouter()
const emit = defineEmits<{
    (e: 'update:toggle'): void
    (e: 'select', language: ILanguage): void
}>()
const { media, subtitle, mediaType } = defineProps<{
    media?: IMovie | IEpisode
    subtitle?: ISubtitle
    mediaType?: MediaType
}>()
const settingsStore = useSettingStore()
const translateStore = useTranslateStore()

const tooltip = ref<InstanceType<typeof TooltipComponent> | null>(null)
const isOpen: Ref<boolean> = ref(false)
const clickOutside: Ref = ref(null)
const excludeClickOutside: Ref = ref(null)

const languages: ComputedRef<ILanguage[]> = computed(
    () => settingsStore.getSetting('target_languages') as ILanguage[]
)

const navigateToDetails = computed(
    () => settingsStore.getSetting(SETTINGS.NAVIGATE_TO_DETAILS_ON_REQUEST) === 'true'
)

const toggle = () => {
    emit('update:toggle')
    isOpen.value = !isOpen.value
}

const selectOption = async (target: ILanguage) => {
    if (media && subtitle && mediaType) {
        const requestId = await translateStore.translateSubtitle(
            media.id,
            subtitle,
            subtitle.language,
            target,
            mediaType
        )
        toggle()
        if (navigateToDetails.value) {
            router.push({ name: 'translation-detail', params: { id: requestId } })
        } else {
            tooltip.value?.showTooltip()
        }
    } else {
        isOpen.value = false
        emit('select', target)
    }
}

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)
</script>

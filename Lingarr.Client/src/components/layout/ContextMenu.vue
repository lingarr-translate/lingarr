<template>
    <div class="relative inline-flex items-center select-none">
        <TooltipComponent ref="tooltip" alignment="left">
            <div ref="clickOutside" @click="toggle" @keydown.esc.stop.prevent="close(true)">
                <button
                    v-if="subtitle"
                    type="button"
                    class="min-h-11 cursor-pointer rounded-lg"
                    :aria-label="`Translate ${subtitle.language.toUpperCase()} subtitles`"
                    :aria-expanded="isOpen"
                    :aria-controls="menuId">
                    <slot></slot>
                </button>
                <slot v-else :is-open="isOpen"></slot>
            </div>
        </TooltipComponent>
        <Teleport to="body">
            <div
                v-if="isOpen"
                :id="menuId"
                ref="excludeClickOutside"
                class="ui-panel text-primary-content fixed z-50 w-56 max-w-[calc(100vw-1rem)] overflow-y-auto p-2"
                :style="menuPosition"
                role="group"
                aria-label="Translate to"
                @keydown.esc.stop.prevent="close(true)"
                @keydown="moveFocus"
                @focusout="handleFocusOut">
                <p
                    class="text-secondary-content px-3 py-2 text-xs font-semibold tracking-wide uppercase">
                    Translate to
                </p>
                <button
                    v-for="language in languages"
                    :key="language.code"
                    type="button"
                    class="hover:bg-accent/15 flex min-h-11 w-full cursor-pointer items-center rounded-lg px-3 py-2 text-left text-sm"
                    @click="selectOption(language)">
                    {{ language.name }}
                </button>
                <router-link
                    v-if="!languages?.length"
                    :to="{ name: 'services-settings' }"
                    class="block rounded-lg px-3 py-2 text-sm underline"
                    @click="close()">
                    Configure target languages in Settings.
                </router-link>
            </div>
        </Teleport>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, computed, ComputedRef, useId, nextTick, onMounted, onUnmounted } from 'vue'
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

const menuId = useId()
const menuPosition = ref<Record<string, string>>({})
const close = (restoreFocus = false) => {
    isOpen.value = false
    if (restoreFocus) clickOutside.value?.querySelector('button')?.focus()
}
const toggle = async () => {
    emit('update:toggle')
    if (isOpen.value) {
        close()
        return
    }
    isOpen.value = true
    await nextTick()
    const anchor = clickOutside.value.getBoundingClientRect()
    const availableBelow = window.innerHeight - anchor.bottom - 16
    const below = availableBelow >= 180 || anchor.top < 180
    menuPosition.value = {
        left: `${Math.max(8, Math.min(anchor.left, window.innerWidth - 232))}px`,
        ...(below
            ? { top: `${anchor.bottom + 8}px` }
            : { bottom: `${window.innerHeight - anchor.top + 8}px` }),
        maxHeight: `${Math.max(100, below ? availableBelow : anchor.top - 16)}px`
    }
    await nextTick()
    excludeClickOutside.value?.querySelector('button, a')?.focus({ preventScroll: true })
}
const handleFocusOut = (event: FocusEvent) => {
    if (
        !excludeClickOutside.value?.contains(event.relatedTarget as Node) &&
        !clickOutside.value?.contains(event.relatedTarget as Node)
    )
        close()
}
const moveFocus = (event: KeyboardEvent) => {
    if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return
    const items = Array.from(
        excludeClickOutside.value?.querySelectorAll('button, a') ?? []
    ) as HTMLElement[]
    if (!items.length) return
    event.preventDefault()
    const current = items.indexOf(document.activeElement as HTMLElement)
    const index =
        event.key === 'Home'
            ? 0
            : event.key === 'End'
              ? items.length - 1
              : (current + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length
    items[index].focus()
}
const handleScroll = (event: Event) => {
    if (!excludeClickOutside.value?.contains(event.target as Node)) close()
}
const handleResize = () => close()
onMounted(() => {
    window.addEventListener('scroll', handleScroll, true)
    window.addEventListener('resize', handleResize)
})
onUnmounted(() => {
    window.removeEventListener('scroll', handleScroll, true)
    window.removeEventListener('resize', handleResize)
})

const selectOption = async (target: ILanguage) => {
    if (media && subtitle && mediaType) {
        const requestId = await translateStore.translateSubtitle(
            media.id,
            subtitle,
            subtitle.language,
            target,
            mediaType
        )
        close(true)
        if (navigateToDetails.value) {
            router.push({ name: 'translation-detail', params: { id: requestId } })
        } else {
            tooltip.value?.showTooltip()
        }
    } else {
        close(true)
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

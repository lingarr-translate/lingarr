<template>
    <div
        ref="clickOutside"
        class="relative z-30 inline-block text-left"
        @keydown.esc.stop.prevent="close(true)">
        <button
            ref="trigger"
            v-tooltip="label"
            type="button"
            :class="buttonClass"
            :aria-label="label"
            :aria-expanded="isOpen"
            :aria-controls="panelId"
            @click="toggleDropdown"
            @keydown.down.prevent="openAndFocus">
            <slot name="button"></slot>
        </button>
        <transition name="fade">
            <div
                v-if="isOpen"
                :id="panelId"
                ref="panel"
                :class="getClasses()"
                class="ui-panel absolute right-0 mt-2 max-w-[calc(100vw-2rem)] overflow-hidden"
                @click="closeAfterSelection"
                @keydown="moveFocus"
                @focusout="handleFocusOut">
                <slot name="content"></slot>
            </div>
        </transition>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, useId, nextTick } from 'vue'
import useClickOutside from '@/composables/useClickOutside'

const isOpen = ref(false)
const clickOutside: Ref<HTMLElement | undefined> = ref()
const trigger = ref<HTMLButtonElement>()
const panel = ref<HTMLElement>()
const panelId = useId()
const {
    width = 'medium',
    label = 'Open options',
    buttonClass = 'ui-icon-button'
} = defineProps<{
    width: string
    label?: string
    buttonClass?: string
}>()
const getClasses = () => (width === 'large' ? 'w-80 md:w-96' : width === 'medium' ? 'w-56' : 'w-24')
const close = (restoreFocus = false) => {
    isOpen.value = false
    if (restoreFocus) trigger.value?.focus()
}
const openAndFocus = async () => {
    isOpen.value = true
    await nextTick()
    panel.value?.querySelector<HTMLElement>('button, a[href]')?.focus()
}
const toggleDropdown = () => (isOpen.value ? close() : openAndFocus())
const closeAfterSelection = (event: MouseEvent) => {
    if ((event.target as HTMLElement).closest('button, a[href]')) close(true)
}
const handleFocusOut = (event: FocusEvent) => {
    if (!clickOutside.value?.contains(event.relatedTarget as Node)) close()
}
const moveFocus = (event: KeyboardEvent) => {
    if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return
    const items = Array.from(
        panel.value?.querySelectorAll<HTMLElement>('button:not([disabled]), a[href]') ?? []
    )
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
useClickOutside(clickOutside, () => close())
</script>

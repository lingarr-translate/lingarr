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
                <a
                    v-for="language in languages"
                    :key="language.code"
                    class="mb-1 flex items-center justify-between text-sm"
                    role="menuitem"
                    @click="selectOption(language)">
                    <span class="cursor-pointer py-2">{{ language.name }}</span>
                </a>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, computed, ComputedRef } from 'vue'
import { ILanguage, ISubtitle } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useTranslateStore } from '@/store/translate'
import useClickOutside from '@/composables/useClickOutside'
import TooltipComponent from '@/components/common/TooltipComponent.vue'

const emit = defineEmits(['update:toggle'])
const { subtitle } = defineProps<{
    subtitle: ISubtitle
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

function toggle() {
    emit('update:toggle')
    isOpen.value = !isOpen.value
}

function selectOption(option: ILanguage) {
    translateStore.translateSubtitle(subtitle, option)
    toggle()
    tooltip.value?.showTooltip()
}

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)
</script>

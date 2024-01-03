<template>
    <div class="relative">
        <div
            ref="excludeClickOutside"
            class="cursor-pointer items-center rounded-xl p-2 text-base transition duration-300 ease-in-out hover:bg-neutral-100 hover:shadow-sm hover:dark:bg-neutral-700"
            @click="isOpen = !isOpen">
            <ContextIcon class="h-6 w-6" />
        </div>
        <div
            ref="clickOutside"
            :class="{ hidden: !isOpen }"
            class="absolute right-8 top-8 z-10 w-56 rounded-md border border-neutral-200 bg-white shadow-md dark:border-neutral-800 dark:bg-neutral-700">
            <div class="px-3 py-1" role="menu" aria-orientation="vertical">
                <a
                    href="#"
                    class="mb-1 block rounded-md py-2 text-sm"
                    role="menuitem"
                    @click="openModal = !openModal">
                    Translate to ...
                </a>
                <hr
                    v-if="usedLanguages.length"
                    class="my-2 h-px border-0 bg-neutral-300 text-sm dark:bg-neutral-500" />
                <a
                    v-for="usedLanguage in usedLanguages"
                    :key="usedLanguage.code"
                    class="mb-1 block cursor-pointer rounded-md py-2 text-sm"
                    role="menuitem"
                    @click="selectOption(usedLanguage)">
                    Translate to {{ usedLanguage.name }}
                </a>
            </div>
        </div>
    </div>
    <ModalOverlay v-model="openModal" :item="item" />
</template>

<script setup lang="ts">
import { ref, Ref } from 'vue'
import { IResource } from '@/ts/resource'
import { ILanguage } from '@/ts/language'
import { useLanguageStore } from '@/store/language'
import { useTranslateStore } from '@/store/translate'
import useClickOutside from '@/composables/useClickOutside'
import ContextIcon from '@/components/icons/ContextIcon.vue'
import ModalOverlay from '@/components/ModalOverlay.vue'

interface Props {
    item: IResource
}
const props = defineProps<Props>()

const translateStore = useTranslateStore()
const isOpen: Ref<boolean> = ref(false)
const openModal: Ref<boolean> = ref(false)
const clickOutside: Ref = ref()
const excludeClickOutside: Ref = ref()

const usedLanguages = useLanguageStore().getUsedLanguages

async function selectOption(option: ILanguage) {
    const subtitles = props.item.subtitles
    const subtitle = subtitles.pop() ?? ''
    if (!subtitle || !option.code) return
    await translateStore.translateSubtitles(subtitle, option)
}

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)
</script>

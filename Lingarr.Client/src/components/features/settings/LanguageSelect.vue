<template>
    <div class="relative">
        <div
            ref="excludeClickOutside"
            class="flex h-12 cursor-pointer items-center justify-between rounded-md border border-accent px-4 py-2"
            @click="toggleDropdown">
            <span v-if="selectedItems.length === 0" class="text-gray-400">
                {{ translate('settings.language.languageSelectPlaceholder') }}
            </span>
            <div v-else class="flex max-h-12 flex-wrap gap-2 overflow-auto">
                <span
                    v-for="(item, index) in selectedItems"
                    :key="`${item.code}-${index}`"
                    class="flex cursor-pointer items-center rounded-md bg-accent px-3 py-1 text-sm font-medium text-secondary-content"
                    @click.stop="removeItem(item)">
                    <span class="mr-2 text-accent-content">{{ item.name }}</span>
                    <TimesIcon class="mt-0.5 h-4 w-4" />
                </span>
            </div>
            <CaretRightIcon
                :class="{ 'rotate-90': isOpen }"
                class="arrow-right h-5 w-5 transition-transform duration-200" />
        </div>
        <ul
            v-show="isOpen"
            ref="clickOutside"
            class="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md border border-accent bg-primary shadow-lg">
            <li v-if="!options?.length" class="p-3">
                {{ translate('settings.language.languageSelectTargetNotification') }}
            </li>
            <li v-else class="flex items-center">
                <input
                    ref="searchInput"
                    v-model="searchTerm"
                    class="relative w-full border-b border-accent bg-transparent p-2 outline-none"
                    :placeholder="
                        translate('settings.language.selectOrSearchLanguagePlaceholder')
                    " />

                <span
                    v-if="searchTerm"
                    class="absolute right-0 z-10 flex cursor-pointer items-center p-3"
                    @click="searchTerm = ''">
                    <TimesIcon class="h-4 w-4" />
                </span>
            </li>
            <li
                v-for="(language, index) in filteredLanguages"
                :key="`${language.code}-${index}`"
                class="cursor-pointer px-4 py-2"
                :class="{ 'bg-accent/20': isSelected(language) }"
                @click="selectItem(language)">
                {{ language.name }}
            </li>
        </ul>
    </div>
</template>

<script setup lang="ts">
import { Ref, ref, computed, watch, nextTick } from 'vue'
import { ILanguage } from '@/ts'
import CaretRightIcon from '@/components/icons/CaretRightIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import useClickOutside from '@/composables/useClickOutside'

const {
    options,
    selected = [],
    disabled = false
} = defineProps<{
    options: ILanguage[]
    selected?: ILanguage[]
    disabled?: boolean
}>()

const emit = defineEmits(['update:selected'])

const isOpen: Ref<boolean> = ref(false)
const searchInput: Ref<HTMLInputElement | undefined> = ref()
const selectedItems: Ref<ILanguage[]> = ref(selected)
const clickOutside: Ref<HTMLElement | undefined> = ref()
const excludeClickOutside: Ref<HTMLElement | undefined> = ref()
const searchTerm: Ref<string> = ref('')

const filteredLanguages = computed(() => {
    return options
        .filter((option) => {
            if (!searchTerm.value) return true
            return option.name.toLowerCase().includes(searchTerm.value.toLowerCase())
        })
        .sort((a, b) => a.name.localeCompare(b.name))
})

const toggleDropdown = async () => {
    if (disabled) return
    isOpen.value = !isOpen.value
    await nextTick()
    searchInput.value?.focus()
}

const selectItem = (item: ILanguage) => {
    const index = selectedItems.value.findIndex((language) => language.code === item.code)
    if (index === -1) {
        selectedItems.value.push(item)
    } else {
        selectedItems.value.splice(index, 1)
    }
    emit('update:selected', selectedItems.value)
    isOpen.value = false
}

const removeItem = (item: ILanguage) => {
    const index = selectedItems.value.findIndex((language) => language.code === item.code)
    if (index !== -1) {
        selectedItems.value.splice(index, 1)
        emit('update:selected', selectedItems.value)
    }
}

const isSelected = (item: ILanguage) => {
    return selectedItems.value.some((language) => language.code === item.code)
}

watch(
    () => selected,
    (newValue) => {
        selectedItems.value = newValue
    },
    { deep: true }
)

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)
</script>

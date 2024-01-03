<template>
    <div class="relative w-full">
        <input
            v-model="searchTerm"
            class="w-full rounded-md border bg-transparent p-2 focus:border-blue-300 focus:outline-none focus:ring"
            placeholder="Select or search language" />

        <div v-if="filteredOptions.length" class="absolute mt-1 w-full">
            <ul
                class="divide-y divide-neutral-600 rounded border border-neutral-600 bg-neutral-700 shadow-md">
                <li
                    v-for="(option, index) in filteredOptions"
                    :key="`${option.code}-${index}`"
                    class="cursor-pointer p-2 hover:bg-neutral-600"
                    @click="selectOption(option)">
                    {{ option.name }}
                </li>
            </ul>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useLanguageStore } from '@/store/language'
import { useTranslateStore } from '@/store/translate'
import { IResource } from '@/ts/resource'
import { ILanguage } from '@/ts/language'
import { ref, computed, onMounted, Ref } from 'vue'

interface Props {
    item: IResource
}
const props = defineProps<Props>()

const emit = defineEmits(['select'])
const languageStore = useLanguageStore()
const translateStore = useTranslateStore()
const searchTerm: Ref<string> = ref('')
const options: Ref<ILanguage[]> = ref([])

async function selectOption(option: ILanguage) {
    searchTerm.value = option.name
    const subtitles = props.item.subtitles
    const subtitle = subtitles.pop() ?? ''
    if (!subtitle || !option.code) return
    await translateStore.translateSubtitles(subtitle, option)
    emit('select', option)
}

const filteredOptions = computed(() => {
    const filtered = options.value.filter((option) => {
        if (!option.name) return false
        return option.name.toLowerCase().includes(searchTerm.value.toLowerCase())
    })

    return filtered.slice(0, 20)
})

onMounted(async () => {
    options.value = await languageStore.getLanguages
})
</script>

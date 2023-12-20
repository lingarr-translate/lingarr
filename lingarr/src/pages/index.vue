<template>
    <div class="container mx-auto rounded bg-neutral-800/60 p-8 shadow-lg">
        <div class="flex items-center justify-between py-4">
            <div class="flex gap-4">
                <BackIcon class="h-6 w-6 cursor-pointer" @click="back()" />
                <ReloadIcon class="h-6 w-6 cursor-pointer" @click="reload()" />
            </div>
            <div class="flex gap-x-4">
                <select
                    v-model="targetLanguage"
                    class="block w-full cursor-pointer rounded-lg border border-neutral-200 bg-neutral-900 px-2 text-sm text-neutral-300 outline-none lg:text-base">
                    <option :value="null" disabled hidden>Select language</option>
                    <option
                        v-for="(language, index) in enrichedUsedLanguages"
                        :key="`enrichedUsedLanguages-${index}`"
                        :value="language.code">
                        {{ language.name }}
                    </option>
                    <option v-if="enrichedUsedLanguages.length && uniqueLanguages.length" disabled class="py-1">
                        ----------
                    </option>
                    <option
                        v-for="(language, index) in uniqueLanguages"
                        :key="`language-${index}`"
                        :value="language.code">
                        {{ language.name }}
                    </option>
                </select>

                <button
                    type="button"
                    class="inline-flex items-center rounded-lg border-neutral-200 px-1 text-sm font-medium text-neutral-300 shadow-sm hover:bg-neutral-800 disabled:pointer-events-none disabled:opacity-50 sm:border sm:bg-neutral-900 sm:px-4 sm:py-3"
                    @click.prevent="translate">
                    <span class="hidden sm:block">Translate</span>
                    <LanguageIcon class="sm:hidden" />
                </button>
            </div>
        </div>
        <div
            class="mb-2 flex overflow-x-auto whitespace-nowrap rounded-lg bg-neutral-900 p-2 text-neutral-400">
            <input
                :disabled="resource?.subtitle"
                v-model="searchFilter"
                type="search"
                class="w-full rounded-lg bg-transparent ps-1 text-sm outline-none"
                placeholder="Search current directory" />
            <button
                class="mx-2 text-neutral-400"
                @click="searchFilter = ''"
                :class="{'cursor-pointer hover:text-neutral-300': !resource?.subtitle}"
                :disabled="resource?.subtitle">
                clear
            </button>
        </div>
        <div v-if="filteredResource" class="flex w-full flex-wrap">
            <div v-for="(item, index) in filteredResource" :key="'item' + index" class="w-full">
                <div
                    class="flex w-full cursor-pointer items-center p-2 text-base text-neutral-500 transition duration-300 ease-in-out hover:rounded hover:bg-neutral-900"
                    @click="navigate(item.name)">
                    <div>
                        <FolderIcon
                            v-if="item.type === 'directory'"
                            class="mr-2 h-4 w-4 fill-current text-neutral-400" />
                        <FileIcon v-else class="mr-2 h-4 w-3 fill-current" />
                    </div>
                    <span class="overflow-x-auto whitespace-nowrap text-neutral-300">
                        {{ item.name }}
                    </span>
                </div>
            </div>
        </div>

        <SubtitleTable v-if="resource?.subtitle" :subtitles="resource.subtitle" />
    </div>
</template>

<script setup lang="ts">
import services from '@/services'
import { computed, onBeforeMount, Ref, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLanguageStore } from '@/store/language'
import FileIcon from '@/components/icons/FileIcon.vue'
import FolderIcon from '@/components/icons/FolderIcon.vue'
import BackIcon from '@/components/icons/BackIcon.vue'
import ReloadIcon from '@/components/icons/ReloadIcon.vue'
import SubtitleTable from '@/components/SubtitleTable.vue'
import LanguageIcon from '@/components/icons/LanguageIcon.vue'
import { IList, IResource } from '@/ts/directory'

const uniqueLanguages = useLanguageStore().getUniqueLanguages
const enrichedUsedLanguages = useLanguageStore().getEnrichedUsedLanguages

const route = useRoute()
const router = useRouter()
const searchFilter: Ref<string> = ref('')
const resource: Ref<IResource> = ref({})
const targetLanguage: Ref<string | null> = ref(null)

const filteredResource = computed<IList[]>(() => {
    return resource.value.list?.search<IList[]>('name', searchFilter.value) ?? []
})

async function navigate(name) {
    let path = route.path.endsWith('/') ? route.path + name : route.path + '/' + name
    await router.push({ path: path })
    resource.value = (await services.resource.list(route.path)) as IResource
}

async function back() {
    await router.push({ path: resource.value.parentDirectory })
    resource.value = (await services.resource.list(route.path)) as IResource
}

async function reload() {
    location.reload()
}

async function translate() {
    await useLanguageStore().translateSubtitles(decodeURI(route.path), targetLanguage.value, () => back())
}

onBeforeMount(async () => {
    resource.value = (await services.resource.list(route.path)) as IResource
})
</script>

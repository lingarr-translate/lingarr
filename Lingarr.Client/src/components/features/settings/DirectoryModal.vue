<template>
    <Teleport to="body">
        <Transition
            enter-active-class="transition-all duration-300 ease-in-out"
            leave-active-class="transition-all duration-300 ease-in-out"
            enter-from-class="opacity-0"
            leave-to-class="opacity-0">
            <div
                v-if="isOpen"
                class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
                @click="$emit('close')">
                <Transition
                    enter-active-class="transition-transform duration-300 ease-in-out"
                    leave-active-class="transition-transform duration-300 ease-in-out"
                    enter-from-class="scale-90"
                    leave-to-class="scale-90">
                    <div
                        class="flex h-96 w-full max-w-md flex-col rounded-lg bg-black p-2"
                        @click.stop>
                        <div class="shell-scroll grow overflow-auto text-sm">
                            <div class="mb-2 text-gray-400">
                                <div v-if="currentPath.length > 0" class="flex items-center p-1">
                                    <button class="mr-2" @click="navigateUp">
                                        <svg
                                            class="h-5 w-5"
                                            viewBox="0 0 24 24"
                                            fill="none"
                                            stroke="currentColor"
                                            stroke-width="2"
                                            stroke-linecap="round"
                                            stroke-linejoin="round">
                                            <path d="m12 19-7-7 7-7" />
                                            <path d="M19 12H5" />
                                        </svg>
                                    </button>
                                    <div class="flex items-center p-1">
                                        <template
                                            v-for="(segment, index) in breadcrumbSegments"
                                            :key="index">
                                            <span
                                                :class="{
                                                    hidden:
                                                        index < breadcrumbSegments.length - 3 &&
                                                        breadcrumbSegments.length > 3
                                                }"
                                                class="cursor-pointer"
                                                @click="navigateToPathIndex(index)">
                                                {{ segment }}
                                            </span>
                                            <span
                                                v-if="index < breadcrumbSegments.length - 1"
                                                :class="{
                                                    hidden:
                                                        index < breadcrumbSegments.length - 3 &&
                                                        breadcrumbSegments.length > 3
                                                }"
                                                class="mx-1">
                                                /
                                            </span>
                                        </template>
                                    </div>
                                </div>
                            </div>
                            <div class="select-none space-y-1 text-gray-400">
                                <div
                                    v-for="dir in directoryContents"
                                    :key="dir.fullPath"
                                    class="flex cursor-pointer items-center rounded-md p-1 transition duration-300"
                                    :class="{
                                        'bg-gray-900': highlightedDirectory === dir,
                                        'hover:brightness-150': highlightedDirectory !== dir
                                    }"
                                    @click="highlightDirectory(dir)"
                                    @dblclick="browseDirectory(dir)">
                                    <svg
                                        xmlns="http://www.w3.org/2000/svg"
                                        class="mr-2 h-4 w-4"
                                        viewBox="0 0 24 24"
                                        fill="none"
                                        stroke="currentColor"
                                        stroke-width="2"
                                        stroke-linecap="round"
                                        stroke-linejoin="round">
                                        <path
                                            d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z" />
                                    </svg>
                                    <span class="truncate">{{ dir.name }}</span>
                                </div>
                            </div>
                        </div>
                        <div class="flex justify-end pt-2">
                            <div
                                class="inline-flex cursor-pointer items-center rounded-md border border-gray-600 px-4 py-1 text-gray-400 transition duration-300 hover:border-gray-500 hover:text-gray-400"
                                @click="selectDirectory">
                                Select
                            </div>
                        </div>
                    </div>
                </Transition>
            </div>
        </Transition>
    </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { DirectoryItem } from '@/ts'
import services from '@/services'

const props = defineProps<{
    isOpen: boolean
}>()

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'select', path: string): void
}>()

const currentPath = ref<string>('/')
const fullPath = ref<string[]>([])
const highlightedDirectory = ref<DirectoryItem | null>(null)
const directoryContents = ref<DirectoryItem[]>([])

const updateFullPath = (path: string) => {
    fullPath.value = path === '/' ? [] : path.split('/').filter(Boolean)
}

const browsePath = async (path: string): Promise<void> => {
    try {
        directoryContents.value = await services.directory.get(path)
        currentPath.value = path
        updateFullPath(path)
        highlightedDirectory.value = null
    } catch (error) {
        console.error('Error browsing directory:', error)
    }
}

const loadRootDirectory = async (): Promise<void> => {
    await browsePath('/')
}

const navigateUp = async (): Promise<void> => {
    if (currentPath.value === '/') {
        return
    }
    const parentPath = currentPath.value.split('/').slice(0, -1).join('/') || '/'
    await browsePath(parentPath)
}

const breadcrumbSegments = computed(() => {
    if (currentPath.value === '/') {
        return ['...']
    }
    const segments = currentPath.value.split('/').filter(Boolean)
    return ['...', ...segments]
})

const navigateToPathIndex = async (index: number) => {
    let newPath: string
    if (index === 0) {
        newPath = '/'
    } else {
        newPath = '/' + breadcrumbSegments.value.slice(1, index + 1).join('/')
    }
    await browsePath(newPath)
}

const highlightDirectory = (dir: DirectoryItem) => {
    highlightedDirectory.value = dir
}

const browseDirectory = (dir: DirectoryItem) => {
    if (dir.name === '...') {
        navigateUp()
    } else {
        browsePath(dir.fullPath)
    }
}

const selectDirectory = () => {
    if (highlightedDirectory.value) {
        emit('select', highlightedDirectory.value.fullPath)
    }
}

watch(
    () => props.isOpen,
    (newValue) => {
        if (newValue) {
            loadRootDirectory()
        }
    }
)
</script>

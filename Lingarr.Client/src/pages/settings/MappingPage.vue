<template>
    <div class="w-full">
        <div class="flex items-center justify-between gap-2 bg-tertiary p-4">
            Source mapping
            <div class="flex space-x-4">
                <button
                    class="text-blue-500 transition duration-300 hover:text-blue-600"
                    @click="addMapping">
                    <PlusIcon class="h-5 w-5" />
                </button>
                <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
                <FloppyIcon v-else class="h-5 w-5 cursor-pointer" @click="saveMappings" />
            </div>
        </div>

        <div class="w-full px-4">
            <div class="hidden border-b border-accent font-bold md:grid md:grid-cols-12">
                <div class="col-span-4 px-4 py-2">Source</div>
                <div class="col-span-4 px-4 py-2">Target</div>
                <div class="col-span-3 px-4 py-2">Media Type</div>
                <div class="col-span-1 flex justify-end px-4 py-2"></div>
            </div>
            <div
                v-for="(pair, index) in mappingStore.mappings"
                :key="`pair-${index}`"
                class="rounded-lg py-4 shadow md:grid md:grid-cols-12 md:rounded-none md:border-b md:border-accent md:bg-transparent md:p-0 md:shadow-none">
                <div class="float-right md:hidden">
                    <TimesIcon
                        class="h-5 w-5 cursor-pointer text-red-500 transition duration-300 hover:text-red-600"
                        @click="removeMapping(index)" />
                </div>
                <div class="mb-2 md:col-span-4 md:mb-0 md:px-4 md:py-2">
                    <span class="font-bold md:hidden">Source:&nbsp;</span>
                    <div
                        class="flex items-center"
                        :class="{
                            'justify-between': pair.sourcePath
                        }">
                        <span v-if="pair.sourcePath" class="rounded-md bg-secondary px-2 py-1">
                            {{ pair.sourcePath }}
                        </span>

                        <div
                            class="flex cursor-pointer"
                            :class="{
                                'w-full justify-end': !pair.sourcePath
                            }"
                            @click="openModal('source', index)">
                            <PlusIcon class="h-12 w-5" />
                        </div>
                    </div>
                </div>
                <div class="mb-2 md:col-span-4 md:mb-0 md:px-4 md:py-2">
                    <span class="font-bold md:hidden">Destination:&nbsp;</span>
                    <div
                        class="flex items-center"
                        :class="{
                            'justify-between': pair.destinationPath
                        }">
                        <span v-if="pair.destinationPath" class="rounded-md bg-secondary px-2 py-1">
                            {{ pair.destinationPath }}
                        </span>
                        <div
                            class="flex cursor-pointer"
                            :class="{
                                'w-full justify-end': !pair.destinationPath
                            }"
                            @click="openModal('target', index)">
                            <PlusIcon class="h-12 w-5" />
                        </div>
                    </div>
                </div>
                <div class="mb-2 md:col-span-3 md:mb-0 md:px-4 md:py-2">
                    <span class="font-bold md:hidden">Media Type:&nbsp;</span>
                    <SelectComponent
                        v-model:selected="pair.mediaType"
                        :options="[
                            { value: MEDIA_TYPE.MOVIE, label: MEDIA_TYPE.MOVIE },
                            { value: MEDIA_TYPE.SHOW, label: MEDIA_TYPE.SHOW }
                        ]" />
                </div>

                <div
                    class="hidden items-center justify-between md:col-span-1 md:flex md:justify-end md:px-4 md:py-2">
                    <TimesIcon
                        class="h-5 w-5 cursor-pointer text-red-500 transition duration-300 hover:text-red-600"
                        @click="removeMapping(index)" />
                </div>
            </div>
        </div>
    </div>

    <DirectoryModal :is-open="isModalOpen" @close="isModalOpen = false" @select="handleSelection" />
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useMappingStore } from '@/store/mapping'
import { MEDIA_TYPE } from '@/ts'
import SelectComponent from '@/components/common/SelectComponent.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'
import FloppyIcon from '@/components/icons/FloppyIcon.vue'
import DirectoryModal from '@/components/features/settings/DirectoryModal.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import { delay } from '@/utils/delay'

const mappingStore = useMappingStore()

const loading = ref(false)
const isModalOpen = ref(false)
const currentSide = ref<'source' | 'target'>('source')
const currentPairIndex = ref(0)

const openModal = (side: 'source' | 'target', index: number) => {
    currentSide.value = side
    currentPairIndex.value = index
    isModalOpen.value = true
}

const handleSelection = (path: string) => {
    const updatedMapping = mappingStore.getMappingByIndex(currentPairIndex.value)
    if (currentSide.value === 'source') {
        updatedMapping.sourcePath = path
    } else {
        updatedMapping.destinationPath = path
    }
    mappingStore.updateMapping(currentPairIndex.value, updatedMapping)
    isModalOpen.value = false
}

const saveMappings = async () => {
    if (loading.value) return
    loading.value = true
    await mappingStore.setMappings(mappingStore.mappings)
    await delay(1000)
    loading.value = false
}

const addMapping = () => {
    mappingStore.addMapping({
        sourcePath: '',
        destinationPath: '',
        mediaType: undefined
    })
}

const removeMapping = (index: number) => {
    mappingStore.removeMapping(index)
}

onMounted(async () => {
    await mappingStore.fetchMappings()
})
</script>

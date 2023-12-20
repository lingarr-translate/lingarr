<template>
    <div class="rounded bg-neutral-900 text-sm lg:text-base">
        <div class="flex flex-col">
            <div
                class="flex w-full rounded-t-lg bg-neutral-700 px-4 py-2 font-bold text-neutral-400">
                <div class="w-3/12 sm:w-2/12 md:w-2/12 xl:w-1/12 2xl:w-1/12">Position</div>
                <div class="mr-1 w-4/12 sm:w-4/12 md:w-3/12 xl:w-3/12 2xl:w-2/12">Timestamp</div>
                <div class="w-5/12 sm:w-6/12 md:w-7/12 xl:w-8/12 2xl:w-8/12">Subtitle</div>
            </div>
            <div
                v-for="(item, index) in visibleSubtitles"
                :key="item.start"
                class="flex w-full border-b border-neutral-800 px-4 py-2 text-neutral-500">
                <div class="w-3/12 sm:w-2/12 md:w-2/12 xl:w-1/12 2xl:w-1/12">{{ index }}</div>
                <div
                    class="w-4/12 overflow-x-auto whitespace-nowrap sm:w-4/12 md:w-3/12 xl:w-3/12 2xl:w-2/12">
                    {{ formatTimestamp(item.data.start) }} - {{ formatTimestamp(item.data.end) }}
                </div>
                <div
                    class="w-5/12 overflow-x-auto whitespace-nowrap sm:w-6/12 md:w-7/12 xl:w-8/12 2xl:w-8/12">
                    {{ item.data.text }}
                </div>
            </div>
            <div v-if="loading" class="my-4 text-center font-bold text-neutral-400">Loading...</div>
        </div>

        <IntersectionObserver
            :options="{ rootMargin: '0px', threshold: 0.5 }"
            @intersect="loadMoreSubtitles()" />
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import IntersectionObserver from '@/components/IntersectionObserver.vue'

interface IProps {
    subtitles: []
}
const props = defineProps<IProps>()

const visibleSubtitles = ref([])
const loading = ref(false)

const loadMoreSubtitles = () => {
    loading.value = true

    const startIndex = visibleSubtitles.value.length
    const endIndex = startIndex + 50
    visibleSubtitles.value = visibleSubtitles.value.concat(
        props.subtitles.slice(startIndex, endIndex)
    )

    loading.value = false
}

function formatTimestamp(timestamp) {
    const date = new Date(timestamp);
    const hours = date.getUTCHours();
    const minutes = date.getUTCMinutes();
    const seconds = date.getUTCSeconds();
    const milliseconds = timestamp % 1000;

    return `${padLeft(hours)}:${padLeft(minutes)}:${padLeft(seconds)},${padLeft(milliseconds, 3)}`;
}

function padLeft(value, width = 2, padChar = '0') {
    return value.toString().padStart(width, padChar);
}

</script>

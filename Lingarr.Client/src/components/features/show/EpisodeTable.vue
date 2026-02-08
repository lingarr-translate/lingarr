<template>
    <div class="bg-tertiary text-tertiary-content w-full">
        <div class="border-primary grid grid-cols-12 border-b-2 font-bold">
            <div class="col-span-1 px-4 py-2">
                <span class="hidden lg:block">
                    Episodes
                </span>
                <span class="block lg:hidden">#</span>
            </div>
            <div class="col-span-7 px-4 py-2 md:col-span-5">
                Title
            </div>
            <div class="col-span-4 flex justify-between py-2 pr-4 md:col-span-5">
                <span>Subtitles</span>
                <span class="hidden md:block">
                    Exclude
                </span>
                <span class="block md:hidden">⊘</span>
            </div>
        </div>
        <div v-for="episode in episodes" :key="episode.id" class="grid grid-cols-12">
            <div class="col-span-1 px-4 py-2">
                {{ episode.episodeNumber }}
            </div>
            <div class="col-span-7 px-4 py-2 md:col-span-5">
                {{ episode.title }}
            </div>
            <div class="col-span-4 flex justify-between pr-4 md:col-span-5">
                <div v-if="episode?.fileName" class="flex flex-wrap items-center gap-2">
                    <ContextMenu
                        v-for="(subtitle, jndex) in getSubtitle(episode.fileName)"
                        :key="`${episode.id}-${jndex}`"
                        :media-type="MEDIA_TYPE.EPISODE"
                        :media="episode"
                        :subtitle="subtitle">
                        <BadgeComponent>
                            {{ subtitle.language.toUpperCase() }}
                            <span v-if="subtitle.caption" class="text-primary-content/50">
                                - {{ subtitle.caption.toUpperCase() }}
                            </span>
                        </BadgeComponent>
                    </ContextMenu>
                </div>
                <div class="col-span-1 px-1 py-2 md:col-span-1">
                    <ToggleButton
                        v-model="episode.excludeFromTranslation"
                        size="small"
                        @toggle:update="() => showStore.exclude(MEDIA_TYPE.EPISODE, episode.id)" />
                </div>
            </div>
        </div>
    </div>
</template>
<script setup lang="ts">
import { IEpisode, ISubtitle, MEDIA_TYPE } from '@/ts'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import { useShowStore } from '@/store/show'

const props = defineProps<{
    episodes: IEpisode[]
    subtitles: ISubtitle[]
}>()
const showStore = useShowStore()

const getSubtitle = (fileName: string | null) => {
    if (!fileName) return null
    return props.subtitles
        .filter(
            (subtitle: ISubtitle) =>
                subtitle.fileName.toLocaleLowerCase().includes(fileName.toLocaleLowerCase()) &&
                subtitle.language &&
                subtitle.language.trim() !== ''
        )
        .slice()
        .sort((a, b) => a.language.localeCompare(b.language))
}
</script>

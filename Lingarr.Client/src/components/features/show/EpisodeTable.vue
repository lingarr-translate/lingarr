<template>
    <div class="media-list media-list--episodes bg-tertiary text-tertiary-content w-full px-2">
        <div class="media-table-heading media-library-heading border-b font-semibold">
            <div>#</div>
            <div>Episode</div>
            <div>Subtitles</div>
            <div>Include</div>
        </div>
        <div
            v-for="episode in episodes"
            :key="episode.id"
            class="media-row media-library-row border-b transition-colors">
            <div class="media-episode-number text-secondary-content">
                {{ episode.episodeNumber }}
            </div>
            <div class="media-title flex items-center gap-2">
                <span class="media-episode-inline text-secondary-content">
                    {{ episode.episodeNumber }}.
                </span>
                <span>{{ episode.title }}</span>
                <ActiveTranslationBadge :media-id="episode.id" :media-type="MEDIA_TYPE.EPISODE" />
            </div>
            <div class="media-subtitles flex-wrap items-center gap-2">
                <template v-if="episode?.fileName">
                    <ContextMenu
                        v-for="(subtitle, jndex) in getSubtitle(episode.fileName)"
                        :key="`${episode.id}-${jndex}`"
                        :media-type="MEDIA_TYPE.EPISODE"
                        :media="episode"
                        :subtitle="subtitle">
                        <BadgeComponent>
                            {{ subtitle.language.toUpperCase() }}
                            <span v-if="subtitle.caption" class="text-secondary-content">
                                - {{ subtitle.caption.toUpperCase() }}
                            </span>
                        </BadgeComponent>
                    </ContextMenu>
                </template>
            </div>
            <div class="media-controls">
                <div class="media-control media-include">
                    <ToggleButton
                        v-model="episode.includeInTranslation"
                        :aria-label="`Include ${episode.title} in automatic translation`"
                        size="small"
                        @toggle:update="
                            () =>
                                showStore.include(
                                    MEDIA_TYPE.EPISODE,
                                    episode.id,
                                    episode.includeInTranslation
                                )
                        " />
                    <span class="media-control-label text-secondary-content text-xs font-semibold">
                        Include
                    </span>
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
import ActiveTranslationBadge from '@/components/common/ActiveTranslationBadge.vue'
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

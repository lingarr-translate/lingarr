<template>
    <div v-if="activeTranslations.length > 0" class="flex items-center gap-1">
        <BadgeComponent
            v-if="inProgressCount > 0"
            classes="border-accent bg-tertiary text-primary-content"
            :title="`${inProgressCount} translation${inProgressCount === 1 ? '' : 's'} in progress`">
            <template v-if="inProgressCount > 1">{{ inProgressCount }}&nbsp;</template>running
        </BadgeComponent>
        <BadgeComponent
            v-if="pendingCount > 0"
            classes="border-accent bg-secondary text-primary-content"
            :title="`${pendingCount} translation${pendingCount === 1 ? '' : 's'} pending`">
            <template v-if="pendingCount > 1">{{ pendingCount }}&nbsp;</template>pending
        </BadgeComponent>
    </div>
</template>

<script setup lang="ts">
import { computed, ComputedRef } from 'vue'
import { IActiveTranslation, IShow, MEDIA_TYPE, MediaType, TRANSLATION_STATUS } from '@/ts'
import useTranslationRequestStore from '@/store/translationRequest'
import BadgeComponent from '@/components/common/BadgeComponent.vue'

const props = defineProps<{
    show?: IShow
    mediaId?: number
    mediaType?: MediaType
}>()

const translationRequestStore = useTranslationRequestStore()

const activeTranslations: ComputedRef<IActiveTranslation[]> = computed(() => {
    if (props.show) {
        const show = props.show
        const seasonIds = new Set(show.seasons.map((season) => season.id))
        const episodeIds = new Set(show.seasons.flatMap((season) => season.episodes.map((episode) => episode.id)))
        return translationRequestStore.activeTranslations.filter((activeTranslation) => {
            if (activeTranslation.mediaId === null) {
                return false
            }
            switch (activeTranslation.mediaType) {
                case MEDIA_TYPE.SHOW:
                    return activeTranslation.mediaId === show.id
                case MEDIA_TYPE.SEASON:
                    return seasonIds.has(activeTranslation.mediaId)
                case MEDIA_TYPE.EPISODE:
                    return episodeIds.has(activeTranslation.mediaId)
                default:
                    return false
            }
        })
    }
    if (props.mediaId !== undefined && props.mediaType !== undefined) {
        return translationRequestStore.activeTranslations.filter((activeTranslation) => {
             return activeTranslation.mediaType === props.mediaType && activeTranslation.mediaId === props.mediaId
            }
        )
    }
    return []
})

const inProgressCount: ComputedRef<number> = computed(
    () => activeTranslations.value.filter((activeTranslation) => {
        return activeTranslation.status === TRANSLATION_STATUS.INPROGRESS
    }).length
)

const pendingCount: ComputedRef<number> = computed(
    () => activeTranslations.value.filter((activeTranslation) => {
        return activeTranslation.status === TRANSLATION_STATUS.PENDING
    }).length
)
</script>

<template>
    <div class="min-w-0 px-5 py-6 sm:px-6">
        <div class="flex items-center gap-3">
            <FeatureIcon :kind="kind" class="h-9 w-9" />
            <h3 class="text-secondary-content text-sm font-medium">{{ title }}</h3>
        </div>
        <p class="text-primary-content mt-3 text-2xl font-semibold tracking-tight tabular-nums">
            {{ formatNumber(value) }}
        </p>
    </div>
</template>

<script setup lang="ts">
import FeatureIcon from '@/components/common/FeatureIcon.vue'
interface Props {
    title: string
    kind: string
    value: number
}

withDefaults(defineProps<Props>(), {
    value: 0
})

const formatNumber = (num: number): string => {
    return new Intl.NumberFormat().format(num)
}
</script>

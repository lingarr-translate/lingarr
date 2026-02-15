<template>
    <div class="border-accent/30 overflow-hidden rounded-md border bg-primary">
        <div class="border-accent/20 flex items-center border-b bg-tertiary/50 px-3 py-2">
            <span class="text-secondary-content/50 text-xs font-medium uppercase tracking-wider">
                Preview
            </span>
            <span class="bg-accent/15 text-accent-content/60 ml-auto rounded px-1.5 py-0.5 text-[10px]">
                Live
            </span>
        </div>
        <div class="p-4">
            <StatusMessage v-if="error" :message="error" type="error" />
            <pre
                v-else
                class="overflow-auto text-xs leading-relaxed"><code v-html="highlightedJson"></code></pre>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useTemplatePreview } from '@/composables/useTemplatePreview'
import StatusMessage from '@/components/common/StatusMessage.vue'

const props = defineProps<{
    template: string
    serviceType: string
}>()

const { error, highlightedJson } = useTemplatePreview(props)
</script>

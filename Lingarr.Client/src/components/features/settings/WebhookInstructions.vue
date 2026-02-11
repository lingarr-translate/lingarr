<template>
    <CardComponent title="Webhook">
        <template #description>
            Configure webhooks in Radarr and Sonarr to automatically trigger translations when new
            media is added.
        </template>
        <template #content>
            <div class="flex flex-col space-y-2">
                <span>
                    Go to
                    <b>Settings → Connect → Connections → +</b>
                    and use this URL:
                </span>
                <span class="font-semibold">Radarr</span>
                <code class="bg-accent/20 mt-1 block rounded p-2 text-sm">
                    {{ webhookUrl }}/api/webhook/radarr
                </code>
                <span class="font-semibold">Sonarr</span>
                <code class="bg-accent/20 mt-1 block rounded p-2 text-sm">
                    {{ webhookUrl }}/api/webhook/sonarr
                </code>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import CardComponent from '@/components/common/CardComponent.vue'

const webhookUrl = window.location.origin
</script>

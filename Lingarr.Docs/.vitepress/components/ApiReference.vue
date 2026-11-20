<template>
    <div class="api-reference">
        <p v-if="apiVersions.length === 0" class="api-reference-missing">
            No API definitions found. The docs build downloads them from the GitHub releases.
        </p>
        <div ref="container" />
    </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { withBase } from 'vitepress'
import { data as apiVersions } from './apiVersions.data'
import 'swagger-ui-dist/swagger-ui.css'

const container = ref<HTMLElement>()

onMounted(async () => {
    if (apiVersions.length === 0) {
        return
    }

    const [{ default: SwaggerUIBundle }, { default: SwaggerUIStandalonePreset }] = await Promise.all([
        import('swagger-ui-dist/swagger-ui-bundle.js'),
        import('swagger-ui-dist/swagger-ui-standalone-preset.js')
    ])

    SwaggerUIBundle({
        domNode: container.value,
        urls: apiVersions.map((version) => ({
            name: version,
            url: withBase(`/api/definitions/${version}.json`)
        })),
        'urls.primaryName': apiVersions[0],
        presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
        layout: 'StandaloneLayout',
        supportedSubmitMethods: []
    })
})
</script>

<style>
.api-reference-missing {
    padding: 2rem;
    color: var(--vp-c-text-2);
}
</style>

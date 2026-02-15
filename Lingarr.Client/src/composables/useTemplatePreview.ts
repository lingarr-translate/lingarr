import { computed } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

interface TemplatePreviewProps {
    readonly template: string
    readonly serviceType: string
}

export function useTemplatePreview(props: TemplatePreviewProps) {
    const settingsStore = useSettingStore()

    const sampleValues = computed(() => {
        const prompt = settingsStore.getSetting(SETTINGS.AI_PROMPT) as string || 'Translate from English to Dutch'

        let userMessage = 'The answer to the ultimate question of life, the universe, and everything is 42.'
        if(settingsStore.getSetting(SETTINGS.AI_CONTEXT_PROMPT_ENABLED) == 'true') {
            userMessage = settingsStore.getSetting(SETTINGS.AI_CONTEXT_PROMPT) as string
        }
        return {
            model: props.serviceType || 'aya-expanse',
            systemPrompt: prompt
                .replace('{sourceLanguage}', 'English')
                .replace('{targetLanguage}', 'Dutch'),
            userMessage: userMessage
        }
    })

    function substitute(template: string): string {
        let result = template
        const vals = sampleValues.value
        result = result.replace(/\{model\}/g, escapeJsonValue(vals.model))
        result = result.replace(/\{systemPrompt\}/g, escapeJsonValue(vals.systemPrompt))
        result = result.replace(/\{userMessage\}/g, escapeJsonValue(vals.userMessage))
        return result
    }

    function escapeJsonValue(value: string): string {
        return JSON.stringify(value).slice(1, -1)
    }

    const error = computed<string | null>(() => {
        try {
            const substituted = substitute(props.template)
            JSON.parse(substituted)
            return null
        } catch (e) {
            return `Invalid JSON: ${(e as Error).message}`
        }
    })

    const highlightedJson = computed(() => {
        if (error.value) return ''
        try {
            const substituted = substitute(props.template)
            const parsed = JSON.parse(substituted)
            const pretty = JSON.stringify(parsed, null, 2)
            return syntaxHighlight(pretty)
        } catch {
            return ''
        }
    })

    function syntaxHighlight(json: string): string {
        return json.replace(
            /("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g,
            (match) => {
                let cssVar = '--syntax-number'
                if (/^"/.test(match)) {
                    cssVar = /:$/.test(match) ? '--syntax-key' : '--syntax-string'
                } else if (/true|false|null/.test(match)) {
                    cssVar = '--syntax-keyword'
                }
                return `<span style="color: var(${cssVar})">${match}</span>`
            }
        )
    }

    return {
        error,
        highlightedJson
    }
}

import { computed } from 'vue'
import { SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'

interface TemplatePreviewProps {
    readonly template: string
    readonly serviceType: string
}

export function useTemplatePreview(props: TemplatePreviewProps) {
    const settingsStore = useSettingStore()

    const sampleValues = computed((): Record<string, string> => {
        const useLanguageCodes = settingsStore.getSetting(SETTINGS.LANGUAGE_CODE_FORMAT) === 'true'
        const sourceLang = useLanguageCodes ? 'en' : 'English'
        const targetLang = useLanguageCodes ? 'nl' : 'Dutch'
        const useBatchTranslation =
            settingsStore.getSetting(SETTINGS.USE_BATCH_TRANSLATION) === 'true'

        const lineToTranslate =
            'The answer to the ultimate question of life, the universe, and everything is 42.'

        const values: Record<string, string> = {
            model: props.serviceType || 'aya-expanse',
            sourceLanguage: sourceLang,
            targetLanguage: targetLang,
            lineToTranslate: useBatchTranslation ? '' : lineToTranslate,
            contextBefore: useBatchTranslation
                ? ''
                : 'What is the answer to the ultimate question?',
            contextAfter: useBatchTranslation ? '' : 'That is not an answer we can work with.'
        }

        const prompt =
            (settingsStore.getSetting(SETTINGS.AI_PROMPT) as string) ||
            'Translate from {sourceLanguage} to {targetLanguage}'
        const userPrompt =
            (settingsStore.getSetting(SETTINGS.AI_USER_PROMPT) as string) || '{lineToTranslate}'

        const systemPrompt = replacePlaceholders(prompt, values)
        const userMessage = useBatchTranslation
            ? JSON.stringify([{ position: 1, line: lineToTranslate }])
            : replacePlaceholders(userPrompt, values)
        values.systemPrompt = systemPrompt
        values.userMessage = userMessage
        return values
    })

    function replacePlaceholders(template: string, values: Record<string, string>): string {
        return template.replace(/\{(\w+)\}/g, (match, name) =>
            name in values ? values[name] : match
        )
    }

    function substitute(template: string): string {
        const values = sampleValues.value
        return template.replace(/\{(\w+)\}/g, (match, name) =>
            name in values ? escapeJsonValue(values[name]) : match
        )
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
                if (match.startsWith('"')) {
                    cssVar = match.endsWith(':') ? '--syntax-key' : '--syntax-string'
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

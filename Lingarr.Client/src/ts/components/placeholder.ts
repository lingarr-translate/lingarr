export interface IPlaceholder {
    placeholder: string
    placeholderText: string
    title: string
    description: string
}

export const PLACEHOLDER: Record<string, IPlaceholder> = {
    MODEL: {
        placeholder: '{model}',
        placeholderText: 'insert {model}',
        title: 'Model',
        description: 'The AI model configured for the selected service'
    },
    SYSTEM_PROMPT: {
        placeholder: '{systemPrompt}',
        placeholderText: 'insert {systemPrompt}',
        title: 'System Prompt',
        description: 'The rendered system prompt'
    },
    USER_MESSAGE: {
        placeholder: '{userMessage}',
        placeholderText: 'insert {userMessage}',
        title: 'User Message',
        description: 'The rendered user prompt, or the subtitle batch in batch mode'
    },
    SOURCE_LANGUAGE: {
        placeholder: '{sourceLanguage}',
        placeholderText: 'insert {sourceLanguage}',
        title: 'Source Language',
        description: 'The language the subtitle is translated from'
    },
    TARGET_LANGUAGE: {
        placeholder: '{targetLanguage}',
        placeholderText: 'insert {targetLanguage}',
        title: 'Target Language',
        description: 'The language the subtitle is translated to'
    },
    LINE_TO_TRANSLATE: {
        placeholder: '{lineToTranslate}',
        placeholderText: 'insert {lineToTranslate}',
        title: 'Subtitle Line',
        description: 'The subtitle line that needs to be translated'
    },
    CONTEXT_BEFORE: {
        placeholder: '{contextBefore}',
        placeholderText: 'insert {contextBefore}',
        title: 'Context Before',
        description: 'Subtitles before the subtitle line that can be used as context'
    },
    CONTEXT_AFTER: {
        placeholder: '{contextAfter}',
        placeholderText: 'insert {contextAfter}',
        title: 'Context After',
        description: 'Subtitles after the subtitle line that can be used as context'
    }
}
